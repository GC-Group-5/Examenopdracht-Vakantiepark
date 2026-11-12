using Vakantiepark.Application.Interfaces;
using Vakantiepark.Domain.Entities;
using Vakantiepark.Domain.Interfaces.Repositories;
using Vakantiepark.Shared.DTOs.Price;

namespace Vakantiepark.Application.Services
{
    // US-07: ONE price service for zoeken, boeken and beheer, so the guest
    // always sees the same amount. Each night is priced with the rate of the
    // season that night falls in, so a stay across a season boundary gets a
    // mixed price. A season without a rate for this verblijfstype falls back
    // to the type's default night price. Eindschoonmaak is added once per
    // booking; toeristenbelasting is alle personen × nachten, and for every
    // night the tax rate that was valid on that night is used (new rate = new
    // row in TouristTaxRate, so old nights keep the old rate).
    public class PriceService : IPriceService
    {
        private readonly IRepository<AccommodationType> TypeRepository;
        private readonly IRepository<Season> SeasonRepository;
        private readonly IRepository<AccommodationTypePrice> PriceRepository;
        private readonly IRepository<TouristTaxRate> TouristTaxRateRepository;

        public PriceService(
            IRepository<AccommodationType> typeRepository,
            IRepository<Season> seasonRepository,
            IRepository<AccommodationTypePrice> priceRepository,
            IRepository<TouristTaxRate> touristTaxRateRepository)
        {
            TypeRepository = typeRepository;
            SeasonRepository = seasonRepository;
            PriceRepository = priceRepository;
            TouristTaxRateRepository = touristTaxRateRepository;
        }

        public async Task<PriceSpecificationResponse> CalculateAsync(
            string accommodationTypeSlug,
            DateOnly arrivalDate,
            DateOnly departureDate,
            int numberOfGuests)
        {
            if (numberOfGuests < 1)
                throw new ArgumentException("Er moet minimaal één gast zijn.", nameof(numberOfGuests));

            // De vertrekdag is exclusief: die nacht wordt niet in rekening gebracht.
            int nights = departureDate.DayNumber - arrivalDate.DayNumber;
            if (nights < 1)
                throw new ArgumentException("De vertrekdatum moet na de aankomstdatum liggen.", nameof(departureDate));

            AccommodationType? type = (await TypeRepository.GetAllAsync())
                .SingleOrDefault(t => t.Slug.Equals(accommodationTypeSlug, StringComparison.OrdinalIgnoreCase));
            if (type is null)
                throw new ArgumentException($"Onbekende verblijfstype '{accommodationTypeSlug}'.", nameof(accommodationTypeSlug));

            List<Season> seasons = [.. await SeasonRepository.GetAllAsync()];
            if (seasons.Count == 0)
                throw new InvalidOperationException("Er zijn geen seizoenen ingesteld.");

            List<TouristTaxRate> taxRates = [.. (await TouristTaxRateRepository.GetAllAsync()).OrderBy(rate => rate.ValidFrom)];
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
                if (season is null)
                    throw new InvalidOperationException($"Voor de nacht van {night:dd-MM-yyyy} is geen seizoen ingesteld.");

                // Het geldige tarief van een nacht is het tarief met de nieuwste
                // ValidFrom op of vóór die nacht.
                TouristTaxRate? taxRate = taxRates.LastOrDefault(rate => rate.ValidFrom <= night);
                if (taxRate is null)
                    throw new InvalidOperationException($"Voor de nacht van {night:dd-MM-yyyy} is nog geen geldig toeristenbelastingtarief ingesteld.");

                accommodationPrice += rateBySeasonId.TryGetValue(season.Id, out decimal rate)
                    ? rate
                    : type.DefaultPricePerNight;
                touristTax += numberOfGuests * taxRate.AmountPerPersonPerNight;

                night = night.AddDays(1);
            }

            decimal cleaningFee = type.CleaningFee;

            return new PriceSpecificationResponse
            {
                AccommodationPrice = accommodationPrice,
                CleaningFee = cleaningFee,
                TouristTax = touristTax,
                TotalPrice = accommodationPrice + cleaningFee + touristTax,
            };
        }
    }
}
