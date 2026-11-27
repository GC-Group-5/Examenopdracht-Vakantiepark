using Vakantiepark.Application.Extensions;
using Vakantiepark.Application.Interfaces;
using Vakantiepark.Domain.Entities;
using Vakantiepark.Domain.Interfaces.Repositories;
using Vakantiepark.Shared.DTOs.Cleaning;

namespace Vakantiepark.Application.Services
{
    // US-32: which units become empty on a day and how far their cleaning is.
    public class CleaningService : ICleaningService
    {
        private readonly IBookingRepository BookingRepository;
        private readonly ICleaningRepository CleaningRepository;
        private readonly TimeProvider TimeProvider;

        public CleaningService(IBookingRepository bookingRepository, ICleaningRepository cleaningRepository, TimeProvider timeProvider)
        {
            BookingRepository = bookingRepository;
            CleaningRepository = cleaningRepository;
            TimeProvider = timeProvider;
        }

        public async Task<CleaningDayResponse> GetDayAsync(DateOnly? date)
        {
            DateOnly day = date ?? TimeProvider.GetToday();

            // The same query as the day overview (US-15): cancelled bookings are already left out there.
            List<Booking> bookings = await BookingRepository.GetArrivingOrDepartingOnAsync(day);
            List<Booking> departures = bookings.Where(booking => booking.DepartureDate == day).ToList();
            HashSet<Guid> unitsWithArrivals = bookings
                .Where(booking => booking.ArrivalDate == day)
                .Select(booking => booking.AccommodationUnitId)
                .ToHashSet();

            List<Cleaning> cleanings = await CleaningRepository.GetByBookingIdsAsync(departures.Select(booking => booking.Id).ToList());

            // Sorted by zone, so the cleaner can walk the park zone by zone.
            return new CleaningDayResponse()
            {
                Date = day,
                Units = departures
                    .OrderBy(booking => booking.AccommodationUnit.Zone.Name)
                    .ThenBy(booking => booking.AccommodationUnit.Code)
                    .Select(booking => booking.ToCleaningItem(
                        cleanings.FirstOrDefault(cleaning => cleaning.BookingId == booking.Id),
                        unitsWithArrivals.Contains(booking.AccommodationUnitId)))
                    .ToList(),
            };
        }
    }
}
