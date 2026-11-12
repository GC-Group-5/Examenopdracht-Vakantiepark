using Vakantiepark.Domain.Interfaces.Repositories;
using Vakantiepark.Application.Interfaces;
using Vakantiepark.Application.Extensions;
using Vakantiepark.Shared.DTOs.Accommodation;
using Vakantiepark.Domain.Entities;

namespace Vakantiepark.Application.Services
{
    public class AccommodationService : IAccommodationService
    {
        private readonly IAccommodationUnitRepository AccommodationUnitRepository;
        private readonly IFeatureRepository FeatureRepository;

        public AccommodationService(IAccommodationUnitRepository accommodationUnitRepository, IFeatureRepository featureRepository)
        {
            AccommodationUnitRepository = accommodationUnitRepository;
            FeatureRepository = featureRepository;
        }

        public async Task<List<AccommodationResponse>> GetAllAsync()
        {
            List<Accommodation> accommodations = await AccommodationUnitRepository.GetAllWithDetailsAsync();
            return accommodations.Select(accommodation => accommodation.ToResponse()).ToList();
        }

        public async Task<AccommodationResponse> GetByCodeAsync(string code)
        {
            Accommodation accommodation = await AccommodationUnitRepository.GetByCodeWithDetailsAsync(code.Trim().ToUpperInvariant())
                ?? throw new KeyNotFoundException($"Accommodatie '{code}' bestaat niet.");

            return accommodation.ToResponse();
        }

        public async Task<List<FeatureResponse>> GetFeaturesAsync()
        {
            List<Feature> features = await FeatureRepository.GetAllWithOptionsAsync();
            return features.Select(feature => feature.ToResponse()).ToList();
        }
    }
}
