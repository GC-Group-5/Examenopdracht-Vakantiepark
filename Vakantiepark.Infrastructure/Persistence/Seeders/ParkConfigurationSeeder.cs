using Microsoft.EntityFrameworkCore;
using Vakantiepark.Domain.Entities;
using Vakantiepark.Domain.Enums;

namespace Vakantiepark.Infrastructure.Persistence.Seeders
{
    public static class ParkConfigurationSeeder
    {
        private const decimal TouristTaxPerPersonPerNight = 2.50m;
        private const int HighSeasonMinimumNights = 7;

        // Night rates per type live in the AccommodationSeeder type table (US-02),
        // so prices exist in exactly one place. The season index there is
        // (laag, midden, hoog, najaar), matching SeasonTemplateTable below.

        // Four contiguous seasons per year so no two seasons overlap and every date in
        // the season years has exactly one rate (US-05 acceptance criteria).
        private static readonly (string Name, int RateIndex, int StartMonth, int StartDay, int EndMonth, int EndDay)[] SeasonTemplateTable =
        [
            ("Laagseizoen", 0, 1, 1, 3, 31),
            ("Middenseizoen", 1, 4, 1, 6, 30),
            ("Hoogseizoen", 2, 7, 1, 8, 31),
            ("Najaarseizoen", 3, 9, 1, 12, 31),
        ];

        public static async Task SeedParkConfigurationAsync(this ApplicationDBContext context)
        {
            ParkSettings? settings = await context.ParkSettings.SingleOrDefaultAsync(setting => setting.DeletedAt == null);
            if (settings is null)
            {
                context.ParkSettings.Add(new ParkSettings
                {
                    Id = Guid.NewGuid(),
                    ArrivalTime = new TimeOnly(15, 0),
                    DepartureTime = new TimeOnly(10, 0),
                    TouristTaxPerPersonPerNight = TouristTaxPerPersonPerNight,
                    HighSeasonMinimumNights = HighSeasonMinimumNights,
                    CreatedAt = DateTime.UtcNow,
                });
            }

            List<Season> seasons = await context.Seasons
                .Where(season => season.DeletedAt == null)
                .ToListAsync();

            int currentYear = DateTime.UtcNow.Year;
            foreach (int year in new[] { currentYear, currentYear + 1 })
            {
                foreach ((string name, int rateIndex, int startMonth, int startDay, int endMonth, int endDay) in SeasonTemplateTable)
                {
                    string seasonName = $"{name} {year}";
                    if (seasons.Any(season => season.Name == seasonName))
                        continue;

                    Season season = new()
                    {
                        Id = Guid.NewGuid(),
                        Name = seasonName,
                        StartDate = new DateOnly(year, startMonth, startDay),
                        EndDate = new DateOnly(year, endMonth, endDay),
                        CreatedAt = DateTime.UtcNow,
                    };
                    season.Prices = [];
                    seasons.Add(season);
                    context.Seasons.Add(season);
                }
            }

            List<AccommodationType> types = await context.AccommodationTypes
                .Where(type => type.DeletedAt == null)
                .ToListAsync();
            List<AccommodationTypePrice> prices = await context.AccommodationTypePrices
                .Where(price => price.DeletedAt == null)
                .ToListAsync();
            IReadOnlyDictionary<string, decimal[]> nightRates = AccommodationSeeder.NightRatesByType();

            foreach (AccommodationType seed in AccommodationSeeder.BuildTypes())
            {
                AccommodationType? type = types.FirstOrDefault(candidate => candidate.Name == seed.Name);
                if (type is null)
                {
                    // Runs after the accommodation seeder (US-02), which already
                    // created the 12 types; this stays as a safety net so price rows
                    // always attach to a type, on a fresh database as well as a partial one.
                    types.Add(seed);
                    context.AccommodationTypes.Add(seed);
                    type = seed;
                }
                else if (type.CleaningFee == 0m && seed.CleaningFee > 0m)
                {
                    type.CleaningFee = seed.CleaningFee;
                }

                decimal[] rates = nightRates[type.Name];

                foreach (Season season in seasons)
                {
                    if (prices.Any(price => price.AccommodationTypeId == type.Id && price.SeasonId == season.Id))
                        continue;

                    AccommodationTypePrice price = new()
                    {
                        Id = Guid.NewGuid(),
                        PricePerNight = rates[RateIndexOf(season.Name)],
                        AccommodationTypeId = type.Id,
                        SeasonId = season.Id,
                        CreatedAt = DateTime.UtcNow,
                    };
                    prices.Add(price);
                    context.AccommodationTypePrices.Add(price);
                }
            }

            await context.SaveChangesAsync();
        }

        private static int RateIndexOf(string seasonName)
        {
            foreach ((string name, int rateIndex, int _, int _, int _, int _) in SeasonTemplateTable)
            {
                if (seasonName.StartsWith(name, StringComparison.Ordinal))
                    return rateIndex;
            }

            throw new InvalidOperationException($"Voor seizoen '{seasonName}' is geen tarief bekend.");
        }
    }
}
