using Vakantiepark.Domain.Entities;
using Vakantiepark.Domain.Enums;
using Vakantiepark.Infrastructure.Persistence.Seeders;
using Xunit;

namespace Vakantiepark.UnitTests
{
    public class AccommodationSeederTests
    {
        // Unit codes are type-prefixed, so category derives from the prefix now
        // that the unit has no Category column (it lives on the Type).
        private static readonly string[] PitchPrefixes = ["TB-", "TC-", "CV-", "CP-", "CPXL-"];

        private static bool IsPitch(string code) => PitchPrefixes.Any(prefix => code.StartsWith(prefix, StringComparison.Ordinal));

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
            Assert.Equal(150, units.Count(unit => !IsPitch(unit.Code)));
            Assert.Equal(50, units.Count(unit => IsPitch(unit.Code)));
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
            Assert.Equal(2, berk.MaxPets);
            Assert.Null(berk.Surface);
            Assert.Null(berk.MaxCaravanLengthMeters);
            Assert.Null(berk.ElectricityAmps);
        }

        [Fact]
        public void FeatureKeysByUnit_FirstBerk_HasItsExportFlags()
        {
            IReadOnlyDictionary<string, string[]> featureCodes = AccommodationSeeder.FeatureKeysByUnit();

            string[] codes = featureCodes["BK4-001"];

            Assert.Contains("pets-allowed", codes);
            Assert.Contains("dishwasher", codes);
            Assert.Contains("waterfront", codes);
            Assert.Contains("child-friendly", codes);
            Assert.DoesNotContain("sauna", codes);
            Assert.DoesNotContain("hot-tub", codes);
        }

        [Fact]
        public void FeatureKeysByUnit_MindervalideHome_HasAllAccessibilityFlags()
        {
            IReadOnlyDictionary<string, string[]> featureCodes = AccommodationSeeder.FeatureKeysByUnit();

            string[] codes = featureCodes["WG4M-001"];

            Assert.Contains("wheelchair-accessible", codes);
            Assert.Contains("level-access", codes);
            Assert.Contains("adapted-bathroom", codes);
            Assert.Contains("widened-doors", codes);
            Assert.Contains("adjustable-bed", codes);
        }

        [Fact]
        public void BuildUnits_CaravanPitch_KeepsHalfMetreLengthsExactly()
        {
            List<Accommodation> units = AccommodationSeeder.BuildUnits();

            Accommodation caravan = units.Single(unit => unit.Code == "CV-001");

            Assert.Equal(PitchSurface.ReinforcedGrass, caravan.Surface);
            Assert.Equal(8d, caravan.MaxCaravanLengthMeters);
            Assert.Equal(16, caravan.ElectricityAmps);
            Assert.Equal(WifiCoverage.Good, caravan.WifiCoverage);
        }

        [Fact]
        public void FeatureKeysByUnit_CaravanPitch_HasItsExportFlags()
        {
            IReadOnlyDictionary<string, string[]> featureCodes = AccommodationSeeder.FeatureKeysByUnit();

            string[] codes = featureCodes["CV-001"];

            Assert.Contains("electricity", codes);
            Assert.Contains("car-at-pitch", codes);
            Assert.Contains("near-sanitary", codes);
            Assert.Contains("pets-allowed", codes);
            Assert.DoesNotContain("drain-connection", codes);
        }

        [Fact]
        public void BuildFeatures_HasOneRowPerExportFlag_WithUniqueCodes()
        {
            List<Feature> features = AccommodationSeeder.BuildFeatures();

            Assert.Equal(26, features.Count);
            Assert.Equal(26, features.Select(feature => feature.Key).Distinct().Count());
        }

        [Fact]
        public void BuildFeatures_PetsAllowed_IsTheOnlySharedCategoryFeature()
        {
            List<Feature> features = AccommodationSeeder.BuildFeatures();

            Assert.Null(features.Single(feature => feature.Key == "pets-allowed").Category);
            Assert.All(features.Where(feature => feature.Key != "pets-allowed"), feature => Assert.NotNull(feature.Category));
            Assert.Equal(AccommodationCategory.HolidayHome, features.Single(feature => feature.Key == "sauna").Category);
            Assert.Equal(AccommodationCategory.CampingPitch, features.Single(feature => feature.Key == "electricity").Category);
        }

        [Fact]
        public void FeatureKeysByUnit_CoversEveryUnit_AndOnlyUsesKnownFeatureCodes()
        {
            IReadOnlyDictionary<string, string[]> featureCodes = AccommodationSeeder.FeatureKeysByUnit();
            HashSet<string> knownCodes = [.. AccommodationSeeder.BuildFeatures().Select(feature => feature.Key)];

            Assert.Equal(200, featureCodes.Count);
            Assert.All(featureCodes.Values, codes => Assert.All(codes, code => Assert.Contains(code, knownCodes)));
        }

        [Fact]
        public void FeatureKeysByUnit_RespectsFeatureCategory_PerUnit()
        {
            IReadOnlyDictionary<string, string[]> featureCodes = AccommodationSeeder.FeatureKeysByUnit();
            Dictionary<string, AccommodationCategory?> categories =
                AccommodationSeeder.BuildFeatures().ToDictionary(feature => feature.Key, feature => feature.Category);

            foreach ((string code, string[] keys) in featureCodes)
            {
                foreach (string key in keys)
                {
                    AccommodationCategory? category = categories[key];
                    bool fitsCategory = category is null
                        || category == (IsPitch(code) ? AccommodationCategory.CampingPitch : AccommodationCategory.HolidayHome);
                    Assert.True(fitsCategory, $"{code} heeft feature '{key}' buiten zijn categorie.");
                }
            }
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
