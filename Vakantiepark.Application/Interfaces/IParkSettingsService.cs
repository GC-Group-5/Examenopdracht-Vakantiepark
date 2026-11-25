using Vakantiepark.Shared.DTOs.Park;

namespace Vakantiepark.Application.Interfaces
{
    public interface IParkSettingsService
    {
        Task<ParkSettingsResponse> GetAsync();
        Task UpdateAsync(UpdateParkSettingsRequest request);
        Task AddTouristTaxRateAsync(AddTouristTaxRateRequest request);
        Task CreateSeasonAsync(SaveSeasonRequest request);
        Task UpdateSeasonAsync(Guid id, SaveSeasonRequest request);
        Task DeleteSeasonAsync(Guid id);
        Task UpdateTypePricesAsync(string slug, UpdateTypePricesRequest request);
    }
}
