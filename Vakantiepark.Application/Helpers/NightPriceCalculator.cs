using Vakantiepark.Domain.Entities;

namespace Vakantiepark.Application.Helpers
{
    // The price of one night for one accommodation type (US-05). PriceService and the average
    // price per night on the cards both use it, so they always follow the same season rules.
    public class NightPriceCalculator
    {
        private readonly List<Season> Seasons;
        private readonly Dictionary<Guid, decimal> RateBySeasonId;
        private readonly decimal DefaultPricePerNight;

        // Takes all seasons and all type prices; only the prices of this type are kept.
        public NightPriceCalculator(AccommodationType type, IEnumerable<Season> seasons, IEnumerable<AccommodationTypePrice> prices)
        {
            Seasons = seasons.ToList();
            RateBySeasonId = prices
                .Where(price => price.AccommodationTypeId == type.Id)
                .GroupBy(price => price.SeasonId)
                .ToDictionary(group => group.Key, group => group.First().PricePerNight);
            DefaultPricePerNight = type.DefaultPricePerNight;
        }

        // A night in no season, or in a season without a rate for this type, costs the default price.
        public decimal PriceOf(DateOnly night)
        {
            Season? season = Seasons.FirstOrDefault(candidate => night >= candidate.StartDate && night <= candidate.EndDate);

            return season is not null && RateBySeasonId.TryGetValue(season.Id, out decimal seasonRate)
                ? seasonRate
                : DefaultPricePerNight;
        }

        // Every night from the first up to (not including) the last day counts once,
        // so a long season weighs more than a short one.
        public decimal AveragePerNight(DateOnly firstNight, DateOnly lastDay)
        {
            int nights = lastDay.DayNumber - firstNight.DayNumber;
            decimal total = Enumerable.Range(0, nights)
                .Sum(day => PriceOf(firstNight.AddDays(day)));

            return Math.Round(total / nights, 2, MidpointRounding.AwayFromZero);
        }
    }
}
