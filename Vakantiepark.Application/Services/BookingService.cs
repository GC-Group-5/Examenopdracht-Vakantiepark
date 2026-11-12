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

        public BookingService(
            IBookingRepository bookingRepository,
            IAccommodationUnitRepository accommodationUnitRepository,
            IPriceService priceService,
            IBookingNumberGenerator bookingNumberGenerator)
        {
            BookingRepository = bookingRepository;
            AccommodationUnitRepository = accommodationUnitRepository;
            PriceService = priceService;
            BookingNumberGenerator = bookingNumberGenerator;
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
    }
}
