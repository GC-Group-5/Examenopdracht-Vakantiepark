using Vakantiepark.Shared.DTOs;
using Vakantiepark.Shared.DTOs.Accommodation;
using Vakantiepark.UI.Application.Interfaces;

namespace Vakantiepark.UI.Application.Services
{
    public class SearchService : ISearchService
    {
        private readonly IHttpService HttpService;

        public SearchService(IHttpService httpService)
        {
            HttpService = httpService;
        }

        public async Task<ApiResponse<List<AccommodationResponse>>> GetAccommodationsAsync()
            => await HttpService.GetAsync<List<AccommodationResponse>>("/Accommodation");

        public async Task<ApiResponse<List<ZoneOptionResponse>>> GetZoneOptionsAsync()
            => await HttpService.GetAsync<List<ZoneOptionResponse>>("/Accommodation/zones");

        public async Task<ApiResponse<AccommodationSearchResponse>> SearchAsync(string? accommodationTypeSlug, string? category, DateOnly arrivalDate, DateOnly departureDate, int numberOfGuests, int numberOfPets, bool wheelchairAccessible, Guid? zoneId)
        {
            string url = "/AccommodationSearch"
                + $"?arrivalDate={arrivalDate:yyyy-MM-dd}"
                + $"&departureDate={departureDate:yyyy-MM-dd}"
                + $"&numberOfGuests={numberOfGuests}"
                + $"&numberOfPets={numberOfPets}";

            if (!string.IsNullOrWhiteSpace(accommodationTypeSlug))
                url += $"&accommodationTypeSlug={Uri.EscapeDataString(accommodationTypeSlug)}";

            if (!string.IsNullOrWhiteSpace(category))
                url += $"&category={Uri.EscapeDataString(category)}";

            if (wheelchairAccessible)
                url += "&wheelchairAccessible=true";

            if (zoneId is not null)
                url += $"&zoneId={zoneId}";

            return await HttpService.GetAsync<AccommodationSearchResponse>(url);
        }
    }
}
