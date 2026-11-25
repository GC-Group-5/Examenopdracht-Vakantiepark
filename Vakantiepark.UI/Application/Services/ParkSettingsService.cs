using Vakantiepark.Shared.DTOs;
using Vakantiepark.Shared.DTOs.Park;
using Vakantiepark.UI.Application.Interfaces;

namespace Vakantiepark.UI.Application.Services
{
    public class ParkSettingsService : IParkSettingsService
    {
        private readonly IHttpService HttpService;

        public ParkSettingsService(IHttpService httpService)
        {
            HttpService = httpService;
        }

        public async Task<ApiResponse<ParkSettingsResponse>> GetAsync()
            => await HttpService.GetAsync<ParkSettingsResponse>("/ParkSettings");

        public async Task<ApiResponse<object>> UpdateAsync(UpdateParkSettingsRequest request)
            => await HttpService.PutAsync<object, UpdateParkSettingsRequest>("/ParkSettings", request);

        public async Task<ApiResponse<object>> AddTouristTaxRateAsync(AddTouristTaxRateRequest request)
            => await HttpService.PostAsync<object, AddTouristTaxRateRequest>("/ParkSettings/TouristTaxRates", request);

        public async Task<ApiResponse<object>> CreateSeasonAsync(SaveSeasonRequest request)
            => await HttpService.PostAsync<object, SaveSeasonRequest>("/ParkSettings/Seasons", request);

        public async Task<ApiResponse<object>> UpdateSeasonAsync(Guid id, SaveSeasonRequest request)
            => await HttpService.PutAsync<object, SaveSeasonRequest>($"/ParkSettings/Seasons/{id}", request);

        public async Task<ApiResponse<object>> DeleteSeasonAsync(Guid id)
            => await HttpService.DeleteAsync<object>($"/ParkSettings/Seasons/{id}");

        public async Task<ApiResponse<object>> UpdateTypePricesAsync(string slug, UpdateTypePricesRequest request)
            => await HttpService.PutAsync<object, UpdateTypePricesRequest>($"/ParkSettings/Prices/{slug}", request);
    }
}
