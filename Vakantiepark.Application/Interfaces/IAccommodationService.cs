using Vakantiepark.Shared.DTOs.Accommodation;

namespace Vakantiepark.Application.Interfaces
{
    public interface IAccommodationService
    {
        Task<List<AccommodationResponse>> GetAllAsync();
        Task<AccommodationResponse> GetByCodeAsync(string code);
        Task<List<FeatureResponse>> GetFeaturesAsync();
    }
}
