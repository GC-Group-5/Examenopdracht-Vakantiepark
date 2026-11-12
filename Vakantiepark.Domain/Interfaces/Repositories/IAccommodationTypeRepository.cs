using Vakantiepark.Domain.Entities;

namespace Vakantiepark.Domain.Interfaces.Repositories
{
    public interface IAccommodationTypeRepository : IRepository<AccommodationType>
    {
        // Looks up one verblijfstype by its URL slug; the slug is stored lowercase.
        Task<AccommodationType?> GetBySlugAsync(string slug);
    }
}
