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
        private readonly IRepository<Zone> ZoneRepository;

        public AccommodationService(IAccommodationUnitRepository accommodationUnitRepository, IFeatureRepository featureRepository, IRepository<Zone> zoneRepository)
        {
            AccommodationUnitRepository = accommodationUnitRepository;
            FeatureRepository = featureRepository;
            ZoneRepository = zoneRepository;
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

        public async Task<List<ZoneOptionResponse>> GetZoneOptionsAsync()
        {
            List<Zone> zones = (await ZoneRepository.GetAllAsync()).ToList();

            return zones
                .OrderBy(zone => zone.Category)
                .ThenBy(zone => zone.Name)
                .Select(zone => new ZoneOptionResponse
                {
                    Id = zone.Id,
                    Name = zone.Name,
                    Category = zone.Category.ToString(),
                })
                .ToList();
        }
    }
}
