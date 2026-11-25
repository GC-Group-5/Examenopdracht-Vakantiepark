using Vakantiepark.Domain.Entities;
using Vakantiepark.Domain.Enums;

namespace Vakantiepark.Domain.Interfaces.Repositories
{
    public interface IAccommodationUnitRepository : IRepository<Accommodation>
    {
        /// <summary>
        /// Returns the in-service units of a type with their zone and feature values, ordered
        /// by code. Whether a unit fits the party or is already booked is checked separately.
        /// </summary>
        Task<List<Accommodation>> GetInServiceUnitsAsync(string accommodationTypeSlug, DateOnly arrivalDate, DateOnly departureDate);

        /// <summary>
        /// Returns every unit with its type, zone and feature values, ordered by code.
        /// </summary>
        Task<List<Accommodation>> GetAllWithDetailsAsync();

        Task<Accommodation?> GetByCodeWithDetailsAsync(string code);

        // US-16: the units that match the filters (empty = all), with type, zone and only the
        // maintenance period that covers the date.
        Task<List<Accommodation>> GetForOccupancyAsync(DateOnly date, string? accommodationTypeSlug, Guid? zoneId, AccommodationCategory? category);
    }
}
