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

        public async Task<List<AccommodationTypeResponse>> GetTypesAsync()
        {
            // Holiday homes first, then from small to large (by default price), like the template.
            List<Accommodation> accommodations = await AccommodationUnitRepository.GetAllWithDetailsAsync();
            return accommodations
                .GroupBy(accommodation => accommodation.Type.Slug)
                .OrderBy(units => units.First().Type.Category)
                .ThenBy(units => units.First().Type.DefaultPricePerNight)
                .ThenBy(units => units.First().Type.Name)
                .Select(units => units.ToTypeResponse())
                .ToList();
        }

        public async Task<AccommodationTypeResponse> GetTypeBySlugAsync(string slug)
        {
            List<Accommodation> accommodations = await AccommodationUnitRepository.GetAllWithDetailsAsync();
            IGrouping<string, Accommodation>? units = accommodations
                .Where(accommodation => accommodation.Type.Slug.Equals(slug.Trim(), StringComparison.OrdinalIgnoreCase))
                .GroupBy(accommodation => accommodation.Type.Slug)
                .SingleOrDefault();

            if (units is null)
                throw new KeyNotFoundException("Verblijfstype niet gevonden.");

            return units.ToTypeResponse();
        }
    }
}
