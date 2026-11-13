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

        public async Task<List<Guid>> GetIdsInZoneAsync(List<Guid> unitIds, string zoneName)
            => await Set
                .Where(x => unitIds.Contains(x.Id) && x.Zone.Name == zoneName)
                .Select(x => x.Id)
                .ToListAsync();

        public async Task<string?> GetTypeNameBySlugAsync(string accommodationTypeSlug)
            => await Context.AccommodationTypes
                .Where(x => x.Slug == accommodationTypeSlug && x.DeletedAt == null)
                .Select(x => x.Name)
                .SingleOrDefaultAsync();

        public async Task<List<string>> GetZoneNamesByTypeSlugAsync(string accommodationTypeSlug)
            => await Set
                .Where(x => x.Type.Slug == accommodationTypeSlug && x.DeletedAt == null)
                .Select(x => x.Zone.Name)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

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
