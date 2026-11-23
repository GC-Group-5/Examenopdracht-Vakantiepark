using Vakantiepark.Application.Extensions;
using Vakantiepark.Application.Interfaces;
using Vakantiepark.Application.Validations.Park;
using Vakantiepark.Application.Validations.Seasons;
using Vakantiepark.Domain.Entities;
using Vakantiepark.Domain.Interfaces.Repositories;
using Vakantiepark.Shared.DTOs.Park;

namespace Vakantiepark.Application.Services
{
    // US-18: the beheerder changes park rules, tourist tax, seasons and prices. Bookings store their own
    // prices and times, so changes only affect new searches and bookings.
    public class ParkSettingsService : IParkSettingsService
    {
        private readonly IRepository<ParkSettings> ParkSettingsRepository;
        private readonly IRepository<TouristTaxRate> TouristTaxRateRepository;
        private readonly IRepository<Season> SeasonRepository;
        private readonly IRepository<AccommodationTypePrice> PriceRepository;
        private readonly IAccommodationTypeRepository TypeRepository;

        public ParkSettingsService(
            IRepository<ParkSettings> parkSettingsRepository,
            IRepository<TouristTaxRate> touristTaxRateRepository,
            IRepository<Season> seasonRepository,
            IRepository<AccommodationTypePrice> priceRepository,
            IAccommodationTypeRepository typeRepository)
        {
            ParkSettingsRepository = parkSettingsRepository;
            TouristTaxRateRepository = touristTaxRateRepository;
            SeasonRepository = seasonRepository;
            PriceRepository = priceRepository;
            TypeRepository = typeRepository;
        }

        public async Task<ParkSettingsResponse> GetAsync()
        {
            ParkSettings settings = await GetSettingsAsync();
            List<Season> seasons = (await SeasonRepository.GetAllAsync()).OrderBy(season => season.StartDate).ToList();
            List<AccommodationTypePrice> prices = (await PriceRepository.GetAllAsync()).ToList();

            return new ParkSettingsResponse
            {
                Rules = settings.ToRequest(),
                TouristTaxRates = (await TouristTaxRateRepository.GetAllAsync())
                    .OrderBy(rate => rate.ValidFrom)
                    .Select(rate => rate.ToRequest())
                    .ToList(),
                Seasons = seasons.Select(season => season.ToResponse()).ToList(),
                Types = (await TypeRepository.GetAllAsync())
                    .OrderBy(type => type.Name)
                    .Select(type => type.ToPricesResponse(seasons, prices))
                    .ToList(),
            };
        }

        public async Task UpdateAsync(UpdateParkSettingsRequest request)
        {
            ParkSettings settings = await GetSettingsAsync();
            request.Validate();

            request.ApplyTo(settings);
            await ParkSettingsRepository.UpdateAsync(settings);
        }

        public async Task AddTouristTaxRateAsync(AddTouristTaxRateRequest request)
        {
            request.Validate(await TouristTaxRateRepository.GetAllAsync());

            await TouristTaxRateRepository.AddAsync(new TouristTaxRate
            {
                AmountPerPersonPerNight = request.AmountPerPersonPerNight!.Value,
                ValidFrom = request.ValidFrom!.Value,
            });
        }

        public async Task CreateSeasonAsync(SaveSeasonRequest request)
        {
            Season season = new Season();
            request.ApplyTo(season);
            await ValidateSeasonAsync(season);

            await SeasonRepository.AddAsync(season);
        }

        public async Task UpdateSeasonAsync(Guid id, SaveSeasonRequest request)
        {
            Season season = await GetSeasonAsync(id);
            request.ApplyTo(season);
            await ValidateSeasonAsync(season);

            await SeasonRepository.UpdateAsync(season);
        }

        public async Task DeleteSeasonAsync(Guid id)
            => await SeasonRepository.DeleteAsync(await GetSeasonAsync(id));

        public async Task UpdateTypePricesAsync(string slug, UpdateTypePricesRequest request)
        {
            AccommodationType type = await TypeRepository.GetBySlugAsync(slug)
                ?? throw new KeyNotFoundException("Dit verblijfstype bestaat niet.");
            List<AccommodationTypePrice> prices = (await PriceRepository.GetAllAsync())
                .Where(price => price.AccommodationTypeId == type.Id)
                .ToList();
            request.Validate(prices);

            type.DefaultPricePerNight = request.DefaultPricePerNight!.Value;
            type.CleaningFee = request.CleaningFee!.Value;
            await TypeRepository.UpdateAsync(type);

            foreach (SeasonPriceRequest seasonPrice in request.SeasonPrices.Where(seasonPrice => seasonPrice.PricePerNight is not null))
            {
                AccommodationTypePrice? price = prices.FirstOrDefault(price => price.SeasonId == seasonPrice.SeasonId);
                if (price is null)
                {
                    await PriceRepository.AddAsync(new AccommodationTypePrice
                    {
                        AccommodationTypeId = type.Id,
                        SeasonId = seasonPrice.SeasonId,
                        PricePerNight = seasonPrice.PricePerNight!.Value,
                    });
                    continue;
                }

                price.PricePerNight = seasonPrice.PricePerNight!.Value;
                await PriceRepository.UpdateAsync(price);
            }
        }

        private async Task<ParkSettings> GetSettingsAsync()
            => (await ParkSettingsRepository.GetAllAsync()).FirstOrDefault()
                ?? throw new InvalidOperationException("De parkinstellingen zijn niet ingesteld.");

        private async Task<Season> GetSeasonAsync(Guid id)
            => await SeasonRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException("Dit seizoen bestaat niet.");

        private async Task ValidateSeasonAsync(Season season)
        {
            List<Season> existingSeasons = (await SeasonRepository.GetAllAsync()).ToList();
            season.ValidateUniqueName(existingSeasons);
            season.ValidateNoOverlap(existingSeasons);
        }
    }
}
