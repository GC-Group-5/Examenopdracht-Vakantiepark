using Microsoft.EntityFrameworkCore;
using Vakantiepark.Domain.Entities;
using Vakantiepark.Domain.Interfaces.Repositories;
using Vakantiepark.Infrastructure.Persistence;

namespace Vakantiepark.Infrastructure.Repositories
{
    public class CleaningRepository : Repository<Cleaning>, ICleaningRepository
    {
        public CleaningRepository(ApplicationDBContext context)
            : base(context) { }

        public async Task<List<Cleaning>> GetByBookingIdsAsync(List<Guid> bookingIds)
            => await Set
                .AsNoTracking()
                .Where(x => x.DeletedAt == null && bookingIds.Contains(x.BookingId))
                .Include(x => x.User)
                .ToListAsync();
    }
}
