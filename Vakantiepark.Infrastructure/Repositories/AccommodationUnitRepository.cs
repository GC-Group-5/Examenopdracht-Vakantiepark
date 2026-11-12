using Vakantiepark.Domain.Interfaces.Repositories;
using Vakantiepark.Infrastructure.Persistence;
using Vakantiepark.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Vakantiepark.Infrastructure.Repositories
{
    public class AccommodationUnitRepository : Repository<Accommodation>, IAccommodationUnitRepository
    {
        public AccommodationUnitRepository(ApplicationDBContext context)
            : base(context) { }

        // There is no maintenance planning yet, so every unit that is not deleted counts as in service.
        public async Task<List<Guid>> GetInServiceIdsByTypeSlugAsync(string accommodationTypeSlug, DateOnly arrivalDate, DateOnly departureDate)
            => await Set
                .Where(x => x.Type.Slug == accommodationTypeSlug && x.Type.DeletedAt == null && x.DeletedAt == null)
                .Select(x => x.Id)
                .ToListAsync();

        public async Task<List<Accommodation>> GetAllWithDetailsAsync()
            => await WithDetails()
                .Where(x => x.DeletedAt == null)
                .OrderBy(x => x.Code)
                .ToListAsync();

        public async Task<Accommodation?> GetByCodeWithDetailsAsync(string code)
            => await WithDetails()
                .SingleOrDefaultAsync(x => x.Code == code && x.DeletedAt == null);

        // Split query: one query per include instead of one wide join of 200 units x their features.
        private IQueryable<Accommodation> WithDetails()
            => Set
                .AsNoTracking()
                .AsSplitQuery()
                .Include(x => x.Type)
                .Include(x => x.Zone)
                .Include(x => x.Features).ThenInclude(x => x.Feature)
                .Include(x => x.Features).ThenInclude(x => x.Option);
    }
}
