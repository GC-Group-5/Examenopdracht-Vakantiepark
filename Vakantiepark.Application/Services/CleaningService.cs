using Vakantiepark.Application.Extensions;
using Vakantiepark.Application.Interfaces;
using Vakantiepark.Domain.Entities;
using Vakantiepark.Domain.Enums;
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

        public async Task StartAsync(Guid bookingId, Guid userId)
        {
            // A cancelled booking is soft deleted, so it is not found either.
            Booking booking = await BookingRepository.GetByIdAsync(bookingId)
                ?? throw new KeyNotFoundException("Boeking niet gevonden.");

            // Before the departure day the guests are still in the unit.
            if (booking.DepartureDate > TimeProvider.GetToday())
            {
                throw new ArgumentException($"Dit huisje komt pas op {booking.DepartureDate:dd-MM-yyyy} leeg.");
            }

            Cleaning? cleaning = await CleaningRepository.GetByBookingIdAsync(bookingId);
            if (cleaning?.Status == CleaningStatus.Clean)
            {
                throw new ArgumentException("Dit huisje is al schoongemaakt.");
            }

            if (cleaning is { Status: CleaningStatus.InProgress })
            {
                throw new ArgumentException($"{cleaning.User?.FullName ?? "Iemand"} is hier al mee bezig.");
            }

            if (cleaning is null)
            {
                await CleaningRepository.AddAsync(new Cleaning()
                {
                    BookingId = booking.Id,
                    AccommodationUnitId = booking.AccommodationUnitId,
                    Status = CleaningStatus.InProgress,
                    StartedAt = TimeProvider.GetUtcNow().UtcDateTime,
                    UserId = userId,
                });
                return;
            }

            cleaning.Status = CleaningStatus.InProgress;
            cleaning.StartedAt = TimeProvider.GetUtcNow().UtcDateTime;
            cleaning.UserId = userId;
            await CleaningRepository.UpdateAsync(cleaning);
        }

        public async Task FinishAsync(Guid bookingId, Guid userId)
        {
            _ = await BookingRepository.GetByIdAsync(bookingId)
                ?? throw new KeyNotFoundException("Boeking niet gevonden.");

            Cleaning? cleaning = await CleaningRepository.GetByBookingIdAsync(bookingId);
            if (cleaning?.Status == CleaningStatus.Clean)
            {
                throw new ArgumentException("Dit huisje is al schoongemaakt.");
            }

            if (cleaning is null || cleaning.Status != CleaningStatus.InProgress)
            {
                throw new ArgumentException("Begin eerst met schoonmaken.");
            }

            // Whoever finishes is stored, so a colleague can take over when the first cleaner had to leave.
            cleaning.Status = CleaningStatus.Clean;
            cleaning.FinishedAt = TimeProvider.GetUtcNow().UtcDateTime;
            cleaning.UserId = userId;
            await CleaningRepository.UpdateAsync(cleaning);
        }
    }
}
