using Vakantiepark.Shared.DTOs;
using Vakantiepark.Shared.DTOs.Accommodation;

namespace Vakantiepark.UI.Application.Interfaces
{
    public interface ISearchService
    {
        Task<ApiResponse<List<AccommodationResponse>>> GetAccommodationsAsync();
        Task<ApiResponse<List<ZoneOptionResponse>>> GetZoneOptionsAsync();
        Task<ApiResponse<AccommodationSearchResponse>> SearchAsync(string? accommodationTypeSlug, DateOnly arrivalDate, DateOnly departureDate, int numberOfGuests, int numberOfPets, Guid? zoneId);
    }
}
