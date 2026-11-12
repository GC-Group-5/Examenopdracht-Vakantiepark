using Microsoft.EntityFrameworkCore;
using Vakantiepark.Domain.Entities;

namespace Vakantiepark.Infrastructure.Persistence.Seeders
{
    // Park rules, tourist tax, prices per type and the seasons with their own prices (US-05).
    // Runs after AccommodationSeeder, because prices belong to the imported types.
    // Every part is only added when its table is still empty, so the beheerder's changes
    // are never overwritten.
    public static class ParkConfigurationSeeder
    {
        // Test prices: the client has not supplied real rates yet.
        public static readonly IReadOnlyDictionary<string, (decimal DefaultPricePerNight, decimal CleaningFee)> TypeRates =
            new Dictionary<string, (decimal, decimal)>
            {
                ["Berk 4"] = (75m, 75m),
                ["Berk 6"] = (90m, 90m),
                ["Eik 6"] = (95m, 90m),
                ["Eik Luxe 6"] = (120m, 110m),
                ["Beuk 8"] = (130m, 120m),
                ["Wilg 4 Mindervalide"] = (85m, 80m),
                ["Bosvilla 10"] = (175m, 150m),
                ["Tentplaats Basis"] = (15m, 0m),
                ["Tentplaats Comfort"] = (18m, 0m),
                ["Caravanplaats"] = (20m, 0m),
                ["Camperplaats"] = (22m, 0m),
                ["Camperplaats XL"] = (27m, 0m),
            };

        // Periods with their own price: default price × factor, rounded to whole euros.
        // Only the summer counts as high season: the 7-night minimum would make the few
        // days around Pentecost unbookable, but the client does want the high rate there.
        private static readonly (string Name, DateOnly Start, DateOnly End, bool IsHighSeason, decimal Factor)[] Periods =
        [
            ("Voorjaarsvakantie 2026", new DateOnly(2026, 2, 14), new DateOnly(2026, 3, 1), false, 1.15m),
            ("Pinksteren 2026", new DateOnly(2026, 5, 22), new DateOnly(2026, 5, 26), false, 1.40m),
            ("Zomervakantie 2026", new DateOnly(2026, 7, 4), new DateOnly(2026, 8, 30), true, 1.50m),
            ("Herfstvakantie 2026", new DateOnly(2026, 10, 10), new DateOnly(2026, 10, 25), false, 1.15m),
            ("Kerstvakantie 2026", new DateOnly(2026, 12, 19), new DateOnly(2027, 1, 3), false, 1.10m),
            ("Voorjaarsvakantie 2027", new DateOnly(2027, 2, 13), new DateOnly(2027, 2, 28), false, 1.15m),
            ("Pinksteren 2027", new DateOnly(2027, 5, 14), new DateOnly(2027, 5, 18), false, 1.40m),
            ("Zomervakantie 2027", new DateOnly(2027, 7, 3), new DateOnly(2027, 8, 29), true, 1.50m),
            ("Herfstvakantie 2027", new DateOnly(2027, 10, 16), new DateOnly(2027, 10, 31), false, 1.15m),
            ("Kerstvakantie 2027", new DateOnly(2027, 12, 18), new DateOnly(2028, 1, 2), false, 1.10m),
        ];

        public static async Task SeedParkConfigurationAsync(this ApplicationDBContext context, CancellationToken cancellationToken = default)
        {
            DateTime now = DateTime.UtcNow;

            if (!await context.ParkSettings.AnyAsync(cancellationToken))
                context.ParkSettings.Add(BuildParkSettings(now));

            if (!await context.TouristTaxRates.AnyAsync(cancellationToken))
                context.TouristTaxRates.Add(new TouristTaxRate { Id = Guid.NewGuid(), AmountPerPersonPerNight = 2.50m, ValidFrom = new DateOnly(2026, 1, 1), CreatedAt = now });

            List<AccommodationType> types = await context.AccommodationTypes.ToListAsync(cancellationToken);
            ApplyTypeRates(types.Where(type => type.DefaultPricePerNight == 0m));

            if (!await context.Seasons.AnyAsync(cancellationToken))
                context.Seasons.AddRange(BuildSeasons(types, now));

            await context.SaveChangesAsync(cancellationToken);
        }

        public static ParkSettings BuildParkSettings(DateTime now) => new()
        {
            Id = Guid.NewGuid(),
            ArrivalTime = new TimeOnly(15, 0),
            DepartureTime = new TimeOnly(10, 0),
            HighSeasonMinimumNights = 7,
            DepositPercentage = 20m,
            FreeCancellationDays = 30,
            FullChargeCancellationDays = 7,
            CreatedAt = now,
        };

        public static void ApplyTypeRates(IEnumerable<AccommodationType> types)
        {
            foreach (AccommodationType type in types)
            {
                if (!TypeRates.TryGetValue(type.Name, out var rates))
                    throw new InvalidOperationException($"Voor type '{type.Name}' is geen testprijs in de seeder.");

                type.DefaultPricePerNight = rates.DefaultPricePerNight;
                type.CleaningFee = rates.CleaningFee;
            }
        }

        // Expects the types to have their default price already (ApplyTypeRates).
        public static List<Season> BuildSeasons(IReadOnlyList<AccommodationType> types, DateTime now) =>
            Periods.Select(period => new Season
            {
                Id = Guid.NewGuid(),
                Name = period.Name,
                StartDate = period.Start,
                EndDate = period.End,
                IsHighSeason = period.IsHighSeason,
                CreatedAt = now,
                Prices = types.Select(type => new AccommodationTypePrice
                {
                    Id = Guid.NewGuid(),
                    AccommodationTypeId = type.Id,
                    PricePerNight = Math.Round(type.DefaultPricePerNight * period.Factor, MidpointRounding.AwayFromZero),
                    CreatedAt = now,
                }).ToList(),
            }).ToList();
    }
}
