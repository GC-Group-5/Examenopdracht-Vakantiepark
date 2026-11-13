using Vakantiepark.Domain.Interfaces.Repositories;
using Vakantiepark.Infrastructure.Persistence;
using Vakantiepark.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Vakantiepark.Infrastructure.Repositories
{
    public class ParkSettingsRepository : Repository<ParkSettings>, IParkSettingsRepository
    {
        public ParkSettingsRepository(ApplicationDBContext context)
            : base(context) { }

        public async Task<ParkSettings?> GetAsync()
            => await Set
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.DeletedAt == null);
    }
}
