using Vakantiepark.Domain.Entities;

namespace Vakantiepark.Domain.Interfaces.Repositories
{
    public interface IAccommodationUnitRepository : IRepository<Accommodation>
    {
        /// <summary>
        /// Returns the units of a type that are not out of service at any moment in the period.
        /// </summary>
        Task<List<Guid>> GetInServiceIdsByTypeSlugAsync(string accommodationTypeSlug, DateOnly arrivalDate, DateOnly departureDate);

        /// <summary>
        /// Returns every unit with its type, zone and feature values, ordered by code.
        /// </summary>
        Task<List<Accommodation>> GetAllWithDetailsAsync();

        Task<Accommodation?> GetByCodeWithDetailsAsync(string code);

        /// <summary>
        /// Returns the ids from unitIds that lie in the given zone.
        /// </summary>
        Task<List<Guid>> GetIdsInZoneAsync(List<Guid> unitIds, string zoneName);

        Task<string?> GetTypeNameBySlugAsync(string accommodationTypeSlug);

        /// <summary>
        /// Returns the names of the zones that have at least one unit of the type, alphabetically.
        /// </summary>
        Task<List<string>> GetZoneNamesByTypeSlugAsync(string accommodationTypeSlug);
    }
}
