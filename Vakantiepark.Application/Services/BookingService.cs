using Microsoft.Extensions.Logging;
using System.Globalization;
using Vakantiepark.Application.Extensions;
using Vakantiepark.Application.Helpers;
using Vakantiepark.Application.Interfaces;
using Vakantiepark.Application.Validations.Booking;
using Vakantiepark.Domain.Entities;
using Vakantiepark.Domain.Interfaces.Helpers;
using Vakantiepark.Domain.Interfaces.Repositories;
using Vakantiepark.Shared.DTOs.Booking;
using Vakantiepark.Shared.DTOs.Price;

namespace Vakantiepark.Application.Services
{
	public class BookingService : IBookingService
	{
		// A collision is very unlikely with 31^8 possible numbers; this only prevents an endless loop.
		private const int MaxBookingNumberAttempts = 5;

		private readonly IBookingRepository BookingRepository;
		private readonly IAccommodationUnitRepository AccommodationUnitRepository;
		private readonly IParkSettingsRepository ParkSettingsRepository;
		private readonly IPriceService PriceService;
		private readonly IBookingNumberGenerator BookingNumberGenerator;
		private readonly IEmailSender EmailSender;
		private readonly ILogger<BookingService> Logger;

		public BookingService(
			IBookingRepository bookingRepository,
			IAccommodationUnitRepository accommodationUnitRepository,
			IParkSettingsRepository parkSettingsRepository,
			IPriceService priceService,
			IBookingNumberGenerator bookingNumberGenerator,
			IEmailSender emailSender,
			ILogger<BookingService> logger)
		{
			BookingRepository = bookingRepository;
			AccommodationUnitRepository = accommodationUnitRepository;
			ParkSettingsRepository = parkSettingsRepository;
			PriceService = priceService;
			BookingNumberGenerator = bookingNumberGenerator;
			EmailSender = emailSender;
			Logger = logger;
		}

		public async Task<BookingResponse> CreateAsync(CreateBookingRequest request)
		{
			request.ValidateParty();

			List<Guid> availableUnitIds = await GetAvailableUnitIdsAsync(request.AccommodationTypeSlug, request.ArrivalDate, request.DepartureDate);

			request.Validate(availableUnitIds);

			Guid unitId = await ChooseUnitAsync(availableUnitIds, request.PreferredZone);

			ParkSettings settings = await GetParkSettingsAsync();

			PriceSpecificationResponse price = await PriceService.CalculateAsync(
				request.AccommodationTypeSlug,
				request.ArrivalDate,
				request.DepartureDate,
				request.NumberOfGuests);

			string bookingNumber = await GenerateUniqueBookingNumberAsync();

			Booking booking = request.ToEntity(unitId, bookingNumber, price, settings);

			if (!await BookingRepository.TryAddAsync(booking))
				throw new ArgumentException(CreateBookingRequestValidation.NotAvailableMessage);

			string accommodationType = await AccommodationUnitRepository.GetTypeNameBySlugAsync(request.AccommodationTypeSlug)
				?? request.AccommodationTypeSlug;

			await SendConfirmationAsync(booking, accommodationType);
			return booking.ToResponse();
		}

		public async Task<BookingInfoResponse> GetInfoAsync(string accommodationTypeSlug)
		{
			string accommodationType = await AccommodationUnitRepository.GetTypeNameBySlugAsync(accommodationTypeSlug)
				?? throw new KeyNotFoundException("Dit verblijf bestaat niet.");

			List<string> zones = await AccommodationUnitRepository.GetZoneNamesByTypeSlugAsync(accommodationTypeSlug);
			ParkSettings settings = await GetParkSettingsAsync();

			return settings.ToInfoResponse(accommodationType, zones);
		}

		private async Task<List<Guid>> GetAvailableUnitIdsAsync(string accommodationTypeSlug, DateOnly arrivalDate, DateOnly departureDate)
		{
			List<Guid> unitIds = await AccommodationUnitRepository.GetInServiceIdsByTypeSlugAsync(accommodationTypeSlug, arrivalDate, departureDate);
			List<Guid> bookedUnitIds = await BookingRepository.GetUnitIdsWithOverlappingBookingAsync(unitIds, arrivalDate, departureDate);

			return [.. unitIds.Except(bookedUnitIds)];
		}

		// Without a preference any free unit will do; with one, the unit must lie in that zone.
		private async Task<Guid> ChooseUnitAsync(List<Guid> availableUnitIds, string? preferredZone)
		{
			if (string.IsNullOrWhiteSpace(preferredZone))
				return availableUnitIds[0];

			List<Guid> unitIdsInZone = await AccommodationUnitRepository.GetIdsInZoneAsync(availableUnitIds, preferredZone.Trim());
			if (unitIdsInZone.Count == 0)
				throw new ArgumentException(CreateBookingRequestValidation.NotAvailableInZoneMessage);

			return availableUnitIds.First(unitIdsInZone.Contains);
		}

		// The settings row is created by the seeder, so a missing row is a setup error, not a guest error.
		private async Task<ParkSettings> GetParkSettingsAsync()
			=> await ParkSettingsRepository.GetAsync()
				?? throw new InvalidOperationException("The park settings were not found.");

		private async Task<string> GenerateUniqueBookingNumberAsync()
		{
			for (int attempt = 0; attempt < MaxBookingNumberAttempts; attempt++)
			{
				string bookingNumber = BookingNumberGenerator.Generate();

				if (!await BookingRepository.ExistsByBookingNumberIncludingDeletedAsync(bookingNumber))
					return bookingNumber;
			}

			throw new InvalidOperationException("Could not generate a unique booking number.");
		}

		private async Task SendConfirmationAsync(Booking booking, string accommodationType)
		{
			try
			{
				CultureInfo dutch = new CultureInfo("nl-NL");

				RenderedEmail email = EmailTemplate.Render(EmailTemplate.BookingConfirmation, new Dictionary<string, string>
				{
					["Name"] = booking.Name,
					["BookingNumber"] = booking.BookingNumber,
					["Email"] = booking.Email,
					["AccommodationType"] = accommodationType,
					["ArrivalDate"] = booking.ArrivalDate.ToString("dddd d MMMM yyyy", dutch),
					["ArrivalTime"] = booking.ArrivalTime.ToString("HH:mm", dutch),
					["DepartureDate"] = booking.DepartureDate.ToString("dddd d MMMM yyyy", dutch),
					["DepartureTime"] = booking.DepartureTime.ToString("HH:mm", dutch),
					["NumberOfGuests"] = booking.NumberOfGuests.ToString(dutch),
					["AccommodationPrice"] = booking.AccommodationPrice.ToString("C", dutch),
					["CleaningFee"] = booking.CleaningFee.ToString("C", dutch),
					["TouristTax"] = booking.TouristTax.ToString("C", dutch),
					["TotalPrice"] = booking.TotalPrice.ToString("C", dutch)
				});

				await EmailSender.SendAsync(booking.Email, $"Bevestiging van je boeking {booking.BookingNumber}", email.HtmlBody, email.TextBody);
			}
			catch (Exception exception)
			{
				// The booking is already saved, so a failed email must not undo it or hide the booking number.
				Logger.LogError(exception, "Could not send the confirmation email for booking {BookingNumber}.", booking.BookingNumber);
			}
		}
	}
}
