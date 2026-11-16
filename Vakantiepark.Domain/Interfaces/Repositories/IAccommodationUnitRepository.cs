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
        /// Returns the in-service units of a type that fit the travel party, with their zone,
        /// ordered by code. Whether a unit is already booked is checked separately.
        /// </summary>
        Task<List<Accommodation>> GetBookableUnitsAsync(string accommodationTypeSlug, DateOnly arrivalDate, DateOnly departureDate, int numberOfGuests);

        /// <summary>
        /// Returns every unit with its type, zone and feature values, ordered by code.
        /// </summary>
        Task<List<Accommodation>> GetAllWithDetailsAsync();

        Task<Accommodation?> GetByCodeWithDetailsAsync(string code);
    }
}
