using Vakantiepark.Domain.Interfaces.Repositories;
using Vakantiepark.Infrastructure.Persistence;
using Vakantiepark.Domain.Entities;
using Vakantiepark.Domain.Enums;
using Vakantiepark.Domain.Filters;
using Vakantiepark.Domain.Paging;
using Vakantiepark.Infrastructure.Extensions;
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
            => await WithDetails()
                .SingleOrDefaultAsync(x => x.AccessToken == accessToken);

        public async Task<Booking?> GetByIdWithDetailsAsync(Guid id)
            => await WithDetails()
                .SingleOrDefaultAsync(x => x.Id == id);

        // A cancelled booking keeps its row (status "Geannuleerd"); deleted_at frees the unit,
        // because the overlap constraint only counts bookings where deleted_at is null.
        public async Task CancelAsync(Booking booking, decimal cancellationFee)
        {
            booking.CancellationFee = cancellationFee;
            booking.DeletedAt = DateTime.UtcNow;
            booking.UpdatedAt = DateTime.UtcNow;
            await Context.SaveChangesAsync();
        }

        public async Task<List<Guid>> GetUnitIdsWithOverlappingBookingExcludingAsync(List<Guid> unitIds, DateOnly arrivalDate, DateOnly departureDate, Guid excludedBookingId)
            => await Set
                .Where(x => x.DeletedAt == null)
                .Where(x => x.Id != excludedBookingId)
                .Where(x => unitIds.Contains(x.AccommodationUnitId))
                .Where(x => x.ArrivalDate < departureDate && x.DepartureDate > arrivalDate)
                .Select(x => x.AccommodationUnitId)
                .Distinct()
                .ToListAsync();

        // Saves a changed booking and replaces the fellow travellers (the main booker stays).
        // When the overlap constraint refuses it, nothing is saved and the booking stays as it was.
        public async Task<bool> TryUpdateAsync(Booking booking, List<BookingGuest> coTravellers)
        {
            List<BookingGuest> oldCoTravellers = booking.Guests.Where(guest => !guest.IsMainBooker).ToList();
            foreach (BookingGuest guest in oldCoTravellers)
            {
                booking.Guests.Remove(guest);
                Context.Set<BookingGuest>().Remove(guest);
            }

            foreach (BookingGuest guest in coTravellers)
                booking.Guests.Add(guest);

            booking.UpdatedAt = DateTime.UtcNow;

            try
            {
                await Context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.ExclusionViolation })
            {
                Context.ChangeTracker.Clear();
                return false;
            }
        }

        public async Task<PagedResult<Booking>> SearchAsync(BookingSearchFilter filter, DateOnly today, int page, int pageSize)
        {
            IQueryable<Booking> query = Set.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                string pattern = $"%{EscapeLikePattern(filter.Search.Trim())}%";
                query = query.Where(x => EF.Functions.ILike(x.BookingNumber, pattern)
                    || EF.Functions.ILike(x.Name, pattern)
                    || EF.Functions.ILike(x.Email, pattern));
            }

            // The departure day counts as a day in the period: the guest is still there that morning.
            if (filter.From is DateOnly from)
                query = query.Where(x => x.DepartureDate >= from);

            if (filter.To is DateOnly to)
                query = query.Where(x => x.ArrivalDate <= to);

            if (!string.IsNullOrWhiteSpace(filter.AccommodationTypeSlug))
            {
                string slug = filter.AccommodationTypeSlug.Trim().ToLowerInvariant();
                query = query.Where(x => x.AccommodationUnit.Type.Slug == slug);
            }

            query = filter.State switch
            {
                BookingState.Cancelled => query.Where(x => x.DeletedAt != null),
                BookingState.Completed => query.Where(x => x.DeletedAt == null && x.DepartureDate < today),
                BookingState.Confirmed => query.Where(x => x.DeletedAt == null && x.DepartureDate >= today),
                _ => query,
            };

            return await query
                .Include(x => x.AccommodationUnit).ThenInclude(x => x.Type)
                .OrderByDescending(x => x.ArrivalDate)
                .ThenBy(x => x.BookingNumber)
                .ToPagedResultAsync(page, pageSize);
        }

        // % and _ typed by a staff member are searched literally, not as wildcards.
        private static string EscapeLikePattern(string text)
            => text.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

        private IQueryable<Booking> WithDetails()
            => Set
                .Include(x => x.Guests)
                .Include(x => x.AccommodationUnit).ThenInclude(x => x.Type)
                .Include(x => x.AccommodationUnit).ThenInclude(x => x.Zone)
                .Include(x => x.Changes).ThenInclude(x => x.User);
    }
}
