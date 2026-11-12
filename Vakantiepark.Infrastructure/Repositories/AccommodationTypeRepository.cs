using Microsoft.EntityFrameworkCore;
using Vakantiepark.Domain.Entities;
using Vakantiepark.Domain.Interfaces.Repositories;
using Vakantiepark.Infrastructure.Persistence;

namespace Vakantiepark.Infrastructure.Repositories
{
    public class AccommodationTypeRepository : Repository<AccommodationType>, IAccommodationTypeRepository
    {
        public AccommodationTypeRepository(ApplicationDBContext context)
            : base(context) { }

        public async Task<AccommodationType?> GetBySlugAsync(string slug)
            => await Set
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.Slug == slug && x.DeletedAt == null);
    }
}
