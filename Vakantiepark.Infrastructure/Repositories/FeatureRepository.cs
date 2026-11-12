using Vakantiepark.Domain.Interfaces.Repositories;
using Vakantiepark.Infrastructure.Persistence;
using Vakantiepark.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Vakantiepark.Infrastructure.Repositories
{
    public class FeatureRepository : Repository<Feature>, IFeatureRepository
    {
        public FeatureRepository(ApplicationDBContext context)
            : base(context) { }

        public async Task<List<Feature>> GetAllWithOptionsAsync()
            => await Set
                .AsNoTracking()
                .Include(x => x.Options.Where(option => option.DeletedAt == null).OrderBy(option => option.SortOrder))
                .Where(x => x.DeletedAt == null)
                .OrderBy(x => x.Name)
                .ToListAsync();
    }
}
