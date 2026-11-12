using Vakantiepark.Domain.Interfaces.Repositories;
using Vakantiepark.Infrastructure.Persistence;
using Vakantiepark.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Vakantiepark.Infrastructure.Repositories
{
    public class BookingRepository : Repository<Booking>, IBookingRepository
    {
        public BookingRepository(ApplicationDBContext context)
            : base(context) { }

        public async Task<Booking?> GetByBookingNumberAsync(string bookingNumber)
            => await Set.SingleOrDefaultAsync(x => x.BookingNumber == bookingNumber && x.DeletedAt == null);

        // Deleted bookings keep their number in the unique index, so they count too.
        public async Task<bool> ExistsByBookingNumberIncludingDeletedAsync(string bookingNumber)
            => await Set.AnyAsync(x => x.BookingNumber == bookingNumber);

        // The exclusion constraint (migration AddBookingOverlapConstraint) rejects a booking that
        // overlaps another one for the same unit, even when two requests passed the availability
        // check at the same moment. The rejected booking and its guests are detached, so the
        // caller can try the next free unit with a fresh booking.
        public async Task<bool> TryAddAsync(Booking booking)
        {
            try
            {
                await AddAsync(booking);
                return true;
            }
            catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.ExclusionViolation })
            {
                foreach (BookingGuest guest in booking.Guests)
                    Context.Entry(guest).State = EntityState.Detached;
                Context.Entry(booking).State = EntityState.Detached;
                return false;
            }
        }

        // Departure dates are exclusive: a guest may arrive on the day the previous guest leaves.
        public async Task<List<Guid>> GetUnitIdsWithOverlappingBookingAsync(List<Guid> unitIds, DateOnly arrivalDate, DateOnly departureDate)
            => await Set
                .Where(x => x.DeletedAt == null)
                .Where(x => unitIds.Contains(x.AccommodationUnitId))
                .Where(x => x.ArrivalDate < departureDate && x.DepartureDate > arrivalDate)
                .Select(x => x.AccommodationUnitId)
                .Distinct()
                .ToListAsync();
    }
}
