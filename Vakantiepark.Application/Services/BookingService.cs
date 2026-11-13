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
        private readonly IAccommodationTypeRepository AccommodationTypeRepository;
        private readonly IParkSettingsRepository ParkSettingsRepository;
        private readonly IRepository<Season> SeasonRepository;
        private readonly IPriceService PriceService;
        private readonly IBookingNumberGenerator BookingNumberGenerator;
        private readonly IEmailSender EmailSender;
        private readonly ILogger<BookingService> Logger;

        public BookingService(
            IBookingRepository bookingRepository,
            IAccommodationUnitRepository accommodationUnitRepository,
            IAccommodationTypeRepository accommodationTypeRepository,
            IParkSettingsRepository parkSettingsRepository,
            IRepository<Season> seasonRepository,
            IPriceService priceService,
            IBookingNumberGenerator bookingNumberGenerator,
            IEmailSender emailSender,
            ILogger<BookingService> logger)
        {
            BookingRepository = bookingRepository;
            AccommodationUnitRepository = accommodationUnitRepository;
            AccommodationTypeRepository = accommodationTypeRepository;
            ParkSettingsRepository = parkSettingsRepository;
            SeasonRepository = seasonRepository;
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

            string? minimumNightsError = await GetHighSeasonMinimumNightsErrorAsync(request, settings);
            if (minimumNightsError is not null)
            {
                throw new ArgumentException(minimumNightsError);
            }

            PriceSpecificationResponse price = await PriceService.CalculateAsync(
                request.AccommodationTypeSlug,
                request.ArrivalDate,
                request.DepartureDate,
                request.NumberOfGuests);

            string bookingNumber = await GenerateUniqueBookingNumberAsync();

            Booking booking = request.ToEntity(unitId, bookingNumber, price, settings);

            if (!await BookingRepository.TryAddAsync(booking))
                throw new ArgumentException(CreateBookingRequestValidation.NotAvailableMessage);

            await SendConfirmationAsync(booking, request.AccommodationTypeSlug, price, settings);
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

        // US-07: when the stay period falls (partly) in a high season, the
        // booking must be at least the configured number of nights. The high
        // season is marked on the season itself (Season.IsHighSeason), so
        // this follows the season calendar directly; nights outside every
        // high season count towards the total length as well.
        private async Task<string?> GetHighSeasonMinimumNightsErrorAsync(CreateBookingRequest request, ParkSettings settings)
        {
            if (settings.HighSeasonMinimumNights < 2)
                return null;

            Season? overlappedSeason = (await SeasonRepository.GetAllAsync())
                .Where(s => s.IsHighSeason)
                .FirstOrDefault(s => s.StartDate < request.DepartureDate && request.ArrivalDate <= s.EndDate);
            if (overlappedSeason is null)
                return null;

            int nights = request.DepartureDate.DayNumber - request.ArrivalDate.DayNumber;
            if (nights >= settings.HighSeasonMinimumNights)
                return null;

            return $"In het hoogseizoen ({overlappedSeason.Name}) moet een boeking minimaal {settings.HighSeasonMinimumNights} nachten zijn.";
        }

        private async Task<List<Guid>> GetAvailableUnitIdsAsync(string accommodationTypeSlug, DateOnly arrivalDate, DateOnly departureDate)
        {
            List<Guid> unitIds = await AccommodationUnitRepository.GetInServiceIdsByTypeSlugAsync(accommodationTypeSlug, arrivalDate, departureDate);
            List<Guid> bookedUnitIds = await BookingRepository.GetUnitIdsWithOverlappingBookingAsync(unitIds, arrivalDate, departureDate);

            return unitIds.Except(bookedUnitIds).ToList();
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

        // Everything is inside the try: a mail server that is down is logged,
        // but never undoes the booking or hides the booking number from the guest.
        private async Task SendConfirmationAsync(Booking booking, string accommodationTypeSlug, PriceSpecificationResponse price, ParkSettings settings)
        {
            try
            {
                CultureInfo dutch = new CultureInfo("nl-NL");

                string accommodationType = (await AccommodationTypeRepository.GetBySlugAsync(accommodationTypeSlug))?.Name
                    ?? accommodationTypeSlug;
                string party = string.Join(", ", new[] { booking.Name }.Concat(booking.Travelers.Select(traveler => traveler.Name)));

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
                    ["Party"] = party,
                    ["AccommodationPrice"] = booking.AccommodationPrice.ToString("C", dutch),
                    ["CleaningFee"] = booking.CleaningFee.ToString("C", dutch),
                    ["TouristTax"] = booking.TouristTax.ToString("C", dutch),
                    ["TotalPrice"] = booking.TotalPrice.ToString("C", dutch),
                    ["Deposit"] = price.Deposit.ToString("C", dutch),
                    ["DepositPercentage"] = settings.DepositPercentage.ToString("0.##", dutch),
                    ["FreeCancellationDays"] = settings.FreeCancellationDaysBeforeArrival.ToString(dutch),
                    ["NoRefundDays"] = settings.NoRefundDaysBeforeArrival.ToString(dutch)
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
