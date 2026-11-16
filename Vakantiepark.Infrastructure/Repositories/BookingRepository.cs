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

        public async Task<List<Booking>> GetByUnitIdInPeriodAsync(Guid unitId, DateOnly startDate, DateOnly endDate)
            => await Set
                .AsNoTracking()
                .Where(x => x.DeletedAt == null)
                .Where(x => x.AccommodationUnitId == unitId)
                .Where(x => x.ArrivalDate < endDate && x.DepartureDate > startDate)
                .OrderBy(x => x.ArrivalDate)
                .ToListAsync();

        public async Task<Booking?> GetByAccessTokenAsync(string accessToken)
            => await Set
                .Include(x => x.Guests)
                .Include(x => x.AccommodationUnit).ThenInclude(x => x.Type)
                .SingleOrDefaultAsync(x => x.AccessToken == accessToken);

        public async Task<Booking?> GetByBookingNumberAndEmailAsync(string bookingNumber, string email)
            => await Set
                .Include(x => x.Guests)
                .Include(x => x.AccommodationUnit).ThenInclude(x => x.Type)
                .SingleOrDefaultAsync(x => x.BookingNumber == bookingNumber.Trim().ToUpper()
                    && x.Email.ToLower() == email.Trim().ToLower());

        public async Task CancelAsync(Booking booking, decimal cancellationFee)
        {
            booking.CancellationFee = cancellationFee;
            booking.DeletedAt = DateTime.UtcNow;
            booking.UpdatedAt = DateTime.UtcNow;
            await Context.SaveChangesAsync();
        }
    }
}