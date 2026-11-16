using Vakantiepark.Domain.Filters;
using Vakantiepark.Shared.DTOs.Accommodation;

namespace Vakantiepark.Application.Interfaces
{
    public interface IAccommodationSearchService
    {
        Task<AccommodationSearchResponse> SearchAsync(string? accommodationTypeSlug, string? category, DateOnly arrivalDate, DateOnly departureDate, AccommodationUnitFilter filter, Guid? zoneId);
    }
}
