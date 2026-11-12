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
    }
}
