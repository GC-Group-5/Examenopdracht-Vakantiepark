using Vakantiepark.Domain.Interfaces.Repositories;
using Vakantiepark.Infrastructure.Persistence;
using Vakantiepark.Domain.Entities;
using Vakantiepark.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Vakantiepark.Infrastructure.Repositories
{
    public class AccommodationUnitRepository : Repository<Accommodation>, IAccommodationUnitRepository
    {
        public AccommodationUnitRepository(ApplicationDBContext context)
            : base(context) { }

        public async Task<List<Accommodation>> GetInServiceUnitsAsync(string accommodationTypeSlug, DateOnly arrivalDate, DateOnly departureDate)
            => await InService(accommodationTypeSlug, arrivalDate, departureDate)
                .AsNoTracking()
                .AsSplitQuery()
                .Include(x => x.Zone)
                .Include(x => x.Features).ThenInclude(x => x.Feature)
                .OrderBy(x => x.Code)
                .ToListAsync();

        public async Task<List<Accommodation>> GetAllWithDetailsAsync()
            => await WithDetails()
                .Where(x => x.DeletedAt == null)
                .OrderBy(x => x.Code)
                .ToListAsync();

        public async Task<Accommodation?> GetByCodeWithDetailsAsync(string code)
            => await WithDetails()
                .SingleOrDefaultAsync(x => x.Code == code && x.DeletedAt == null);

        public async Task<List<Accommodation>> GetForOccupancyAsync(DateOnly date, string? accommodationTypeSlug, Guid? zoneId, AccommodationCategory? category)
            => await Set
                .AsNoTracking()
                .Where(x => x.DeletedAt == null)
                .Where(x => string.IsNullOrEmpty(accommodationTypeSlug) || x.Type.Slug == accommodationTypeSlug)
                .Where(x => zoneId == null || x.ZoneId == zoneId)
                .Where(x => category == null || x.Type.Category == category)
                .Include(x => x.Type)
                .Include(x => x.Zone)
                .Include(x => x.MaintenancePeriods.Where(period => period.DeletedAt == null && period.StartDate <= date && period.EndDate >= date))
                .OrderBy(x => x.Code)
                .ToListAsync();

        // A unit counts as in service when it is not deleted and no maintenance period
        // (US-17) covers any night of the stay; same overlap rule as a booking clash.
        private IQueryable<Accommodation> InService(string accommodationTypeSlug, DateOnly arrivalDate, DateOnly departureDate)
            => Set.Where(x =>
                x.Type.Slug == accommodationTypeSlug &&
                x.Type.DeletedAt == null &&
                x.DeletedAt == null &&
                !x.MaintenancePeriods.Any(period =>
                    period.DeletedAt == null &&
                    period.StartDate < departureDate &&
                    period.EndDate >= arrivalDate));

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
