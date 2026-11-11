namespace Vakantiepark.Domain.Interfaces.Repositories
{
    /// <summary>
    /// Implemented with US-06 Zoeken, once accommodation types and units exist.
    /// Returns the units of a type that are not out of service at any moment in the period.
    /// </summary>
    public interface IAccommodationUnitRepository
    {
        Task<List<Guid>> GetInServiceIdsByTypeSlugAsync(string accommodationTypeSlug, DateOnly arrivalDate, DateOnly departureDate);
    }
}
