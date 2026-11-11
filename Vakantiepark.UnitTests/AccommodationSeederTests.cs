using Vakantiepark.Domain.Entities;
using Vakantiepark.Domain.Enums;
using Vakantiepark.Infrastructure.Persistence.Seeders;
using Xunit;

namespace Vakantiepark.UnitTests
{
    public class AccommodationSeederTests
    {
        [Fact]
        public void BuildTypes_ContainsEveryExportType_Once()
        {
            List<AccommodationType> types = AccommodationSeeder.BuildTypes();

            Assert.Equal(12, types.Count);
            Assert.Equal(12, types.Select(type => type.Name).Distinct().Count());
            Assert.Equal(12, types.Select(type => type.Slug).Distinct().Count());
        }

        [Fact]
        public void BuildTypes_MindervalideType_IsHolidayHome()
        {
            List<AccommodationType> types = AccommodationSeeder.BuildTypes();

            AccommodationType mindervalide = types.Single(type => type.Name == "Wilg 4 Mindervalide");

            Assert.Equal(AccommodationCategory.HolidayHome, mindervalide.Category);
            Assert.Equal("wilg-4-mindervalide", mindervalide.Slug);
        }

        [Fact]
        public void BuildTypes_CleaningFee_IsPositiveForHomes_AndZeroForPitches()
        {
            List<AccommodationType> types = AccommodationSeeder.BuildTypes();

            Assert.All(types.Where(type => type.Category == AccommodationCategory.HolidayHome), type => Assert.True(type.CleaningFee > 0m));
            Assert.All(types.Where(type => type.Category == AccommodationCategory.CampingPitch), type => Assert.Equal(0m, type.CleaningFee));
        }

        [Fact]
        public void BuildUnits_ContainsEveryExportUnit_WithUniqueCodes()
        {
            List<Accommodation> units = AccommodationSeeder.BuildUnits();

            Assert.Equal(200, units.Count);
            Assert.Equal(200, units.Select(unit => unit.Code).Distinct().Count());
            Assert.Equal(150, units.Count(unit => unit.Category == AccommodationCategory.HolidayHome));
            Assert.Equal(50, units.Count(unit => unit.Category == AccommodationCategory.CampingPitch));
        }

        [Fact]
        public void BuildUnits_PerTypeCounts_MatchTheExportFile()
        {
            List<Accommodation> units = AccommodationSeeder.BuildUnits();

            Assert.Equal(32, units.Count(unit => unit.Code.StartsWith("BK4-")));
            Assert.Equal(26, units.Count(unit => unit.Code.StartsWith("BK6-")));
            Assert.Equal(24, units.Count(unit => unit.Code.StartsWith("EK6-")));
            Assert.Equal(18, units.Count(unit => unit.Code.StartsWith("EL6-")));
            Assert.Equal(20, units.Count(unit => unit.Code.StartsWith("BU8-")));
            Assert.Equal(12, units.Count(unit => unit.Code.StartsWith("WG4M-")));
            Assert.Equal(18, units.Count(unit => unit.Code.StartsWith("BV10-")));
            Assert.Equal(12, units.Count(unit => unit.Code.StartsWith("TB-")));
            Assert.Equal(10, units.Count(unit => unit.Code.StartsWith("TC-")));
            Assert.Equal(12, units.Count(unit => unit.Code.StartsWith("CV-")));
            Assert.Equal(10, units.Count(unit => unit.Code.StartsWith("CP-")));
            Assert.Equal(6, units.Count(unit => unit.Code.StartsWith("CPXL-")));
        }

        [Fact]
        public void BuildUnits_FirstBerk_HasTheExportValues()
        {
            List<Accommodation> units = AccommodationSeeder.BuildUnits();

            Accommodation berk = units.Single(unit => unit.Code == "BK4-001");

            Assert.Equal("Rustweide", berk.Zone);
            Assert.Equal(4, berk.MaxPersons);
            Assert.Equal(2, berk.Bedrooms);
            Assert.Equal(1, berk.Bathrooms);
            Assert.Equal(4, berk.Beds);
            Assert.True(berk.PetsAllowed);
            Assert.Equal(2, berk.MaxPets);
            Assert.True(berk.Dishwasher);
            Assert.True(berk.Waterfront);
            Assert.True(berk.ChildFriendly);
            Assert.False(berk.Sauna);
            Assert.False(berk.WheelchairAccessible);
        }

        [Fact]
        public void BuildUnits_MindervalideHome_HasAllAccessibilityFeatures()
        {
            List<Accommodation> units = AccommodationSeeder.BuildUnits();

            Accommodation wilg = units.Single(unit => unit.Code == "WG4M-001");

            Assert.True(wilg.WheelchairAccessible);
            Assert.True(wilg.LevelAccess);
            Assert.True(wilg.AdaptedBathroom);
            Assert.True(wilg.WidenedDoors);
            Assert.True(wilg.AdjustableBed);
        }

        [Fact]
        public void BuildUnits_CaravanPitch_KeepsHalfMetreLengthsExactly()
        {
            List<Accommodation> units = AccommodationSeeder.BuildUnits();

            Accommodation caravan = units.Single(unit => unit.Code == "CV-001");

            Assert.Equal(PitchSurface.ReinforcedGrass, caravan.Surface);
            Assert.Equal(8d, caravan.MaxCaravanLengthMeters);
            Assert.Equal(16, caravan.ElectricityAmps);
            Assert.False(caravan.DrainConnection);
            Assert.Equal(WifiCoverage.Good, caravan.WifiCoverage);
        }

        [Fact]
        public void BuildUnits_Pitches_HaveNoHomeFeatures()
        {
            List<Accommodation> units = AccommodationSeeder.BuildUnits();

            Assert.All(units.Where(unit => unit.Category == AccommodationCategory.CampingPitch), unit =>
            {
                Assert.Equal(0, unit.Bedrooms);
                Assert.Equal(0, unit.Beds);
                Assert.False(unit.Sauna);
                Assert.False(unit.Waterfront);
            });
        }

        [Fact]
        public void BuildUnits_Homes_HaveNoPitchFeatures()
        {
            List<Accommodation> units = AccommodationSeeder.BuildUnits();

            Assert.All(units.Where(unit => unit.Category == AccommodationCategory.HolidayHome), unit =>
            {
                Assert.Null(unit.Surface);
                Assert.Null(unit.MaxCaravanLengthMeters);
                Assert.False(unit.Electricity);
            });
        }

        [Fact]
        public void NightRatesByType_HasFourRatesPerType_WithHighSeasonAsTheMostExpensive()
        {
            IReadOnlyDictionary<string, decimal[]> rates = AccommodationSeeder.NightRatesByType();

            Assert.Equal(12, rates.Count);
            Assert.All(rates.Values, values =>
            {
                Assert.Equal(4, values.Length);
                Assert.True(values[2] > values[0]);
            });
        }
    }
}
