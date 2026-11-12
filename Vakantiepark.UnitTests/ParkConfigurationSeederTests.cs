using Vakantiepark.Application.Validations.Seasons;
using Vakantiepark.Domain.Entities;
using Vakantiepark.Infrastructure.Persistence.Seeders;
using Xunit;

namespace Vakantiepark.UnitTests
{
    public class ParkConfigurationSeederTests
    {
        private static readonly DateTime Now = new(2026, 11, 12, 9, 0, 0, DateTimeKind.Utc);

        private static List<AccommodationType> TypesWithRates()
        {
            List<AccommodationType> types = AccommodationSeeder.BuildImport(AccommodationSeeder.LoadExport(), Now).Types;
            ParkConfigurationSeeder.ApplyTypeRates(types);
            return types;
        }

        [Fact]
        public void ApplyTypeRates_EveryExportType_GetsAPositiveDefaultPrice()
        {
            List<AccommodationType> types = TypesWithRates();

            Assert.All(types, type => Assert.True(type.DefaultPricePerNight > 0m, type.Name));
        }

        [Fact]
        public void ApplyTypeRates_UnknownType_Throws()
        {
            AccommodationType unknown = TestData.CreateAccommodationType(type => type.Name = "Boomhut 2");

            Assert.Throws<InvalidOperationException>(() => ParkConfigurationSeeder.ApplyTypeRates([unknown]));
        }

        [Fact]
        public void BuildSeasons_DoNotOverlap()
        {
            List<Season> seasons = ParkConfigurationSeeder.BuildSeasons(TypesWithRates(), Now);

            foreach (Season season in seasons)
                season.ValidateNoOverlap(seasons);
        }

        [Fact]
        public void BuildSeasons_OnlyTheSummerHolidays_AreHighSeason_AndLastAtLeastTheMinimumStay()
        {
            List<Season> seasons = ParkConfigurationSeeder.BuildSeasons(TypesWithRates(), Now);
            int minimumNights = ParkConfigurationSeeder.BuildParkSettings(Now).HighSeasonMinimumNights;

            List<Season> highSeasons = seasons.Where(season => season.IsHighSeason).ToList();

            Assert.NotEmpty(highSeasons);
            Assert.All(highSeasons, season =>
            {
                Assert.StartsWith("Zomervakantie", season.Name);
                Assert.True(season.EndDate.DayNumber - season.StartDate.DayNumber + 1 >= minimumNights);
            });
        }

        [Fact]
        public void BuildSeasons_EverySeason_HasAPricePerType_AboveTheDefaultPrice()
        {
            List<AccommodationType> types = TypesWithRates();

            List<Season> seasons = ParkConfigurationSeeder.BuildSeasons(types, Now);

            Assert.All(seasons, season =>
            {
                Assert.Equal(types.Count, season.Prices.Count);
                Assert.All(season.Prices, price =>
                    Assert.True(price.PricePerNight > types.Single(type => type.Id == price.AccommodationTypeId).DefaultPricePerNight));
            });
        }

        [Fact]
        public void BuildParkSettings_UsesTheRulesAgreedWithTheClient()
        {
            ParkSettings settings = ParkConfigurationSeeder.BuildParkSettings(Now);

            Assert.Equal(new TimeOnly(15, 0), settings.ArrivalTime);
            Assert.Equal(new TimeOnly(10, 0), settings.DepartureTime);
            Assert.Equal(7, settings.HighSeasonMinimumNights);
            Assert.Equal(20m, settings.DepositPercentage);
            Assert.Equal(30, settings.FreeCancellationDays);
            Assert.Equal(7, settings.FullChargeCancellationDays);
        }
    }
}
