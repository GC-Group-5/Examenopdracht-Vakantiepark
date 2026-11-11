using Vakantiepark.Domain.Interfaces.Repositories;
using Vakantiepark.Infrastructure.Persistence;
using Vakantiepark.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Vakantiepark.Infrastructure.Repositories
{
    public class BookingRepository : Repository<Booking>, IBookingRepository
    {
        public BookingRepository(ApplicationDBContext context)
            : base(context) { }

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
