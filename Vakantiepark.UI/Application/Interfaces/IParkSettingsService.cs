using Vakantiepark.Shared.DTOs;
using Vakantiepark.Shared.DTOs.Park;

namespace Vakantiepark.UI.Application.Interfaces
{
    public interface IParkSettingsService
    {
        Task<ApiResponse<ParkSettingsResponse>> GetAsync();
        Task<ApiResponse<object>> UpdateAsync(UpdateParkSettingsRequest request);
        Task<ApiResponse<object>> AddTouristTaxRateAsync(AddTouristTaxRateRequest request);
        Task<ApiResponse<object>> CreateSeasonAsync(SaveSeasonRequest request);
        Task<ApiResponse<object>> UpdateSeasonAsync(Guid id, SaveSeasonRequest request);
        Task<ApiResponse<object>> DeleteSeasonAsync(Guid id);
        Task<ApiResponse<object>> UpdateTypePricesAsync(string slug, UpdateTypePricesRequest request);
    }
}
