using Vakantiepark.Domain.Interfaces.Repositories;
using Vakantiepark.Infrastructure.Persistence;
using Vakantiepark.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Vakantiepark.Infrastructure.Repositories
{
    public class SwimSlotRepository : Repository<SwimSlot>, ISwimSlotRepository
    {
        public SwimSlotRepository(ApplicationDBContext context)
            : base(context) { }

        public async Task<List<SwimSlot>> GetByDateAsync(DateOnly date)
            => await Set
                .Where(x => x.Date == date && x.DeletedAt == null)
                .OrderBy(x => x.StartTime)
                .ToListAsync();

        public async Task<bool> TryAddRangeAsync(List<SwimSlot> slots)
        {
            DateTime now = DateTime.UtcNow;
            foreach (SwimSlot slot in slots)
                slot.CreatedAt = now;

            try
            {
                Set.AddRange(slots);
                await Context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                foreach (SwimSlot slot in slots)
                    Context.Entry(slot).State = EntityState.Detached;
                return false;
            }
        }
    }
}
