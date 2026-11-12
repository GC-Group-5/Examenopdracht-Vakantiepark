using Vakantiepark.Application.Interfaces;
using Vakantiepark.Domain.Entities;
using Vakantiepark.Domain.Interfaces.Repositories;
using Vakantiepark.Shared.DTOs.Price;

namespace Vakantiepark.Application.Services
{
    // US-07: ONE price service for zoeken, boeken and beheer, so the guest
    // always sees the same amount. Each night is priced with the rate of the
    // season that night falls in, so a stay across a season boundary gets a
    // mixed price. A night that falls in no season, in a season without a
    // rate for this verblijfstype, or when no seasons are configured at all,
    // falls back to the type's default night price. Eindschoonmaak is added
    // once per booking; toeristenbelasting is alle personen × nachten, and
    // for every night the tax rate that was valid on that night is used (a
    // new rate is a new TouristTaxRate row, so old nights keep the old rate).
    // The deposit is the configured percentage of the total.
    public class PriceService : IPriceService
    {
        private readonly IAccommodationTypeRepository TypeRepository;
        private readonly IRepository<Season> SeasonRepository;
        private readonly IRepository<AccommodationTypePrice> PriceRepository;
        private readonly IRepository<TouristTaxRate> TouristTaxRateRepository;
        private readonly IRepository<ParkSettings> ParkSettingsRepository;

        public PriceService(
            IAccommodationTypeRepository typeRepository,
            IRepository<Season> seasonRepository,
            IRepository<AccommodationTypePrice> priceRepository,
            IRepository<TouristTaxRate> touristTaxRateRepository,
            IRepository<ParkSettings> parkSettingsRepository)
        {
            TypeRepository = typeRepository;
            SeasonRepository = seasonRepository;
            PriceRepository = priceRepository;
            TouristTaxRateRepository = touristTaxRateRepository;
            ParkSettingsRepository = parkSettingsRepository;
        }

        public async Task<PriceSpecificationResponse> CalculateAsync(
            string accommodationTypeSlug,
            DateOnly arrivalDate,
            DateOnly departureDate,
            int numberOfGuests)
        {
            if (numberOfGuests < 1)
                throw new ArgumentException("Er moet minimaal één gast zijn.", nameof(numberOfGuests));

            // The departure day is excluded: that night is not billed.
            int nights = departureDate.DayNumber - arrivalDate.DayNumber;
            if (nights < 1)
                throw new ArgumentException("De vertrekdatum moet na de aankomstdatum liggen.", nameof(departureDate));

            AccommodationType? type = await TypeRepository.GetBySlugAsync(accommodationTypeSlug);
            if (type is null)
                throw new ArgumentException($"Onbekende verblijfstype '{accommodationTypeSlug}'.", nameof(accommodationTypeSlug));

            List<Season> seasons = (await SeasonRepository.GetAllAsync()).ToList();
            List<TouristTaxRate> taxRates = (await TouristTaxRateRepository.GetAllAsync())
                .OrderBy(rate => rate.ValidFrom)
                .ToList();
            if (taxRates.Count == 0)
                throw new InvalidOperationException("Er is nog geen toeristenbelastingtarief ingesteld.");

            Dictionary<Guid, decimal> rateBySeasonId = (await PriceRepository.GetAllAsync())
                .Where(price => price.AccommodationTypeId == type.Id)
                .GroupBy(price => price.SeasonId)
                .ToDictionary(group => group.Key, group => group.First().PricePerNight);

            decimal accommodationPrice = 0m;
            decimal touristTax = 0m;
            DateOnly night = arrivalDate;

            for (int i = 0; i < nights; i++)
            {
                Season? season = seasons.FirstOrDefault(s => night >= s.StartDate && night <= s.EndDate);

                // Acceptance criterion: a night that falls in no season
                // is priced with the type's default night price.
                accommodationPrice += season is null
                    ? type.DefaultPricePerNight
                    : rateBySeasonId.TryGetValue(season.Id, out decimal seasonRate)
                        ? seasonRate
                        : type.DefaultPricePerNight;

                // The valid tax rate of a night is the rate with the newest
                // ValidFrom on or before that night.
                TouristTaxRate? taxRate = taxRates.LastOrDefault(tax => tax.ValidFrom <= night);
                if (taxRate is null)
                    throw new InvalidOperationException($"Voor de nacht van {night:dd-MM-yyyy} is nog geen geldig toeristenbelastingtarief ingesteld.");

                touristTax += numberOfGuests * taxRate.AmountPerPersonPerNight;

                night = night.AddDays(1);
            }

            decimal cleaningFee = type.CleaningFee;
            decimal totalPrice = accommodationPrice + cleaningFee + touristTax;

            ParkSettings? settings = (await ParkSettingsRepository.GetAllAsync()).FirstOrDefault();
            if (settings is null)
                throw new InvalidOperationException("De parkinstellingen zijn niet ingesteld.");

            decimal deposit = Math.Round(totalPrice * settings.DepositPercentage / 100m, 2, MidpointRounding.AwayFromZero);

            return new PriceSpecificationResponse
            {
                AccommodationPrice = accommodationPrice,
                CleaningFee = cleaningFee,
                TouristTax = touristTax,
                TotalPrice = totalPrice,
                Deposit = deposit,
            };
        }
    }
}
