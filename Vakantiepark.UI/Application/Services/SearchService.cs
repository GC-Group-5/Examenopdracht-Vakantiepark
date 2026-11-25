using Vakantiepark.Shared.DTOs;
using Vakantiepark.Shared.DTOs.Accommodation;
using Vakantiepark.UI.Application.Helpers;
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
            string url = new QueryString()
                .Add("arrivalDate", arrivalDate)
                .Add("departureDate", departureDate)
                .Add("numberOfGuests", numberOfGuests)
                .Add("numberOfPets", numberOfPets)
                .Add("accommodationTypeSlug", accommodationTypeSlug)
                .Add("category", category)
                .Add("wheelchairAccessible", wheelchairAccessible)
                .Add("zoneId", zoneId)
                .ToUrl("/AccommodationSearch");

            return await HttpService.GetAsync<AccommodationSearchResponse>(url);
        }
    }
}
