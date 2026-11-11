using Vakantiepark.Application.Validations.Booking;
using Vakantiepark.Domain.Interfaces.Repositories;
using Vakantiepark.Application.Interfaces;
using Vakantiepark.Application.Extensions;
using Vakantiepark.Shared.DTOs.Booking;
using Vakantiepark.Shared.DTOs.Price;
using Vakantiepark.Domain.Entities;

namespace Vakantiepark.Application.Services
{
    public class BookingService : IBookingService
    {
        private readonly IBookingRepository BookingRepository;
        private readonly IAccommodationUnitRepository AccommodationUnitRepository;
        private readonly IPriceService PriceService;

        public BookingService(
            IBookingRepository bookingRepository,
            IAccommodationUnitRepository accommodationUnitRepository,
            IPriceService priceService)
        {
            BookingRepository = bookingRepository;
            AccommodationUnitRepository = accommodationUnitRepository;
            PriceService = priceService;
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

            Booking booking = request.ToEntity(availableUnitIds[0], price);
            await BookingRepository.AddAsync(booking);

            return booking.ToResponse();
        }

        private async Task<List<Guid>> GetAvailableUnitIdsAsync(string accommodationTypeSlug, DateOnly arrivalDate, DateOnly departureDate)
        {
            List<Guid> unitIds = await AccommodationUnitRepository.GetInServiceIdsByTypeSlugAsync(accommodationTypeSlug, arrivalDate, departureDate);
            List<Guid> bookedUnitIds = await BookingRepository.GetUnitIdsWithOverlappingBookingAsync(unitIds, arrivalDate, departureDate);

            return [.. unitIds.Except(bookedUnitIds)];
        }
    }
}
