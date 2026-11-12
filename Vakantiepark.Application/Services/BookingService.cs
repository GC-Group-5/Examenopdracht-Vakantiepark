using Vakantiepark.Application.Validations.Booking;
using Vakantiepark.Domain.Interfaces.Repositories;
using Vakantiepark.Domain.Interfaces.Helpers;
using Vakantiepark.Application.Interfaces;
using Vakantiepark.Application.Extensions;
using Vakantiepark.Shared.DTOs.Booking;
using Vakantiepark.Shared.DTOs.Price;
using Vakantiepark.Domain.Entities;

namespace Vakantiepark.Application.Services
{
    public class BookingService : IBookingService
    {
        // A collision is very unlikely with 31^8 possible numbers; this only prevents an endless loop.
        private const int MaxBookingNumberAttempts = 5;

        private readonly IBookingRepository BookingRepository;
        private readonly IAccommodationUnitRepository AccommodationUnitRepository;
        private readonly IPriceService PriceService;
        private readonly IBookingNumberGenerator BookingNumberGenerator;
        private readonly IRepository<Season> SeasonRepository;
        private readonly IRepository<ParkSettings> ParkSettingsRepository;

        public BookingService(
            IBookingRepository bookingRepository,
            IAccommodationUnitRepository accommodationUnitRepository,
            IPriceService priceService,
            IBookingNumberGenerator bookingNumberGenerator,
            IRepository<Season> seasonRepository,
            IRepository<ParkSettings> parkSettingsRepository)
        {
            BookingRepository = bookingRepository;
            AccommodationUnitRepository = accommodationUnitRepository;
            PriceService = priceService;
            BookingNumberGenerator = bookingNumberGenerator;
            SeasonRepository = seasonRepository;
            ParkSettingsRepository = parkSettingsRepository;
        }

        public async Task<BookingResponse> CreateAsync(CreateBookingRequest request)
        {
            List<Guid> availableUnitIds = await GetAvailableUnitIdsAsync(request.AccommodationTypeSlug, request.ArrivalDate, request.DepartureDate);

            request.Validate(availableUnitIds);

            string? minimumNightsError = await GetHighSeasonMinimumNightsErrorAsync(request);
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

            Booking booking = request.ToEntity(availableUnitIds[0], bookingNumber, price);

            if (!await BookingRepository.TryAddAsync(booking))
                throw new ArgumentException(CreateBookingRequestValidation.NotAvailableMessage);

            return booking.ToResponse();
        }

        // US-07: when the stay period falls (partly) in a high season, the
        // booking must be at least the configured number of nights. The high
        // season is marked on the season itself (Season.IsHighSeason), so
        // this follows the season calendar directly; nights outside every
        // high season count towards the total length as well.
        private async Task<string?> GetHighSeasonMinimumNightsErrorAsync(CreateBookingRequest request)
        {
            ParkSettings? settings = (await ParkSettingsRepository.GetAllAsync()).FirstOrDefault();
            if (settings is null || settings.HighSeasonMinimumNights < 2)
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
    }
}
