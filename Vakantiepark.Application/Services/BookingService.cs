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
		private const int MaxBookingNumberAttempts = 5;

		private readonly IBookingRepository BookingRepository;
		private readonly IAccommodationUnitRepository AccommodationUnitRepository;
		private readonly IPriceService PriceService;
		private readonly IBookingNumberGenerator BookingNumberGenerator;
		private readonly IEmailSender EmailSender;
		private readonly ILogger<BookingService> Logger;

		public BookingService(
			IBookingRepository bookingRepository,
			IAccommodationUnitRepository accommodationUnitRepository,
			IPriceService priceService,
			IBookingNumberGenerator bookingNumberGenerator,
			IEmailSender emailSender,
			ILogger<BookingService> logger)
		{
			BookingRepository = bookingRepository;
			AccommodationUnitRepository = accommodationUnitRepository;
			PriceService = priceService;
			BookingNumberGenerator = bookingNumberGenerator;
			EmailSender = emailSender;
			Logger = logger;
		}

		public async Task<BookingResponse> CreateAsync(CreateBookingRequest request)
		{
			List<Guid> availableUnitIds = await GetAvailableUnitIdsAsync(request.AccommodationTypeSlug, request.ArrivalDate, request.DepartureDate);

			request.Validate(availableUnitIds);

			PriceSpecificationResponse price = await PriceService.CalculateAsync(
				request.AccommodationTypeSlug,
				request.ArrivalDate,
				request.DepartureDate,
				request.NumberOfGuests);

			string bookingNumber = await GenerateUniqueBookingNumberAsync();

			Booking booking = request.ToEntity(availableUnitIds[0], bookingNumber, price);

			if (!await BookingRepository.TryAddAsync(booking))
				throw new ArgumentException(CreateBookingRequestValidation.NotAvailableMessage);
			
			await SendConfirmationAsync(booking, request.AccommodationTypeSlug);
			return booking.ToResponse();
		}

		private async Task<List<Guid>> GetAvailableUnitIdsAsync(string accommodationTypeSlug, DateOnly arrivalDate, DateOnly departureDate)
		{
			List<Guid> unitIds = await AccommodationUnitRepository.GetInServiceIdsByTypeSlugAsync(accommodationTypeSlug, arrivalDate, departureDate);
			List<Guid> bookedUnitIds = await BookingRepository.GetUnitIdsWithOverlappingBookingAsync(unitIds, arrivalDate, departureDate);

			return [.. unitIds.Except(bookedUnitIds)];
		}

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
				Logger.LogError(exception, "Could not send the confirmation email for booking {BookingNumber}.", booking.BookingNumber);
			}
		}
	}
}
