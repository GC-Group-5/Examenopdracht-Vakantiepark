using Vakantiepark.Domain.Interfaces.Repositories;
using Vakantiepark.Application.Interfaces;
using Vakantiepark.Application.Extensions;
using Vakantiepark.Application.Helpers;
using Vakantiepark.Shared.DTOs.Accommodation;
using Vakantiepark.Domain.Entities;

namespace Vakantiepark.Application.Services
{
    public class AccommodationService : IAccommodationService
    {
        private const int AveragePriceMonths = 12;

        private readonly IAccommodationUnitRepository AccommodationUnitRepository;
        private readonly IFeatureRepository FeatureRepository;
        private readonly IRepository<Zone> ZoneRepository;
        private readonly IRepository<Season> SeasonRepository;
        private readonly IRepository<AccommodationTypePrice> PriceRepository;
        private readonly TimeProvider TimeProvider;

        public AccommodationService(
            IAccommodationUnitRepository accommodationUnitRepository,
            IFeatureRepository featureRepository,
            IRepository<Zone> zoneRepository,
            IRepository<Season> seasonRepository,
            IRepository<AccommodationTypePrice> priceRepository,
            TimeProvider timeProvider)
        {
            AccommodationUnitRepository = accommodationUnitRepository;
            FeatureRepository = featureRepository;
            ZoneRepository = zoneRepository;
            SeasonRepository = seasonRepository;
            PriceRepository = priceRepository;
            TimeProvider = timeProvider;
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
            Func<AccommodationType, decimal> averagePricePerNight = await LoadAveragePricePerNightAsync();

            return accommodations
                .GroupBy(accommodation => accommodation.Type.Slug)
                .OrderBy(units => units.First().Type.Category)
                .ThenBy(units => units.First().Type.DefaultPricePerNight)
                .ThenBy(units => units.First().Type.Name)
                .Select(units => units.ToTypeResponse(averagePricePerNight(units.First().Type)))
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

            Func<AccommodationType, decimal> averagePricePerNight = await LoadAveragePricePerNightAsync();
            return units.ToTypeResponse(averagePricePerNight(units.First().Type));
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

        // Seasons and prices are loaded once and shared by every type; a night is priced
        // with the same season rules as a booking (NightPriceCalculator).
        private async Task<Func<AccommodationType, decimal>> LoadAveragePricePerNightAsync()
        {
            List<Season> seasons = (await SeasonRepository.GetAllAsync()).ToList();
            List<AccommodationTypePrice> prices = (await PriceRepository.GetAllAsync()).ToList();
            DateOnly today = DateOnly.FromDateTime(TimeProvider.GetUtcNow().UtcDateTime);
            DateOnly lastDay = today.AddMonths(AveragePriceMonths);

            return type => new NightPriceCalculator(type, seasons, prices).AveragePerNight(today, lastDay);
        }
    }
}
