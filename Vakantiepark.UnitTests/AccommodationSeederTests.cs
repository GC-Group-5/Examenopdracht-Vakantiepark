using Vakantiepark.Domain.Constants;
using Vakantiepark.Domain.Entities;
using Vakantiepark.Domain.Enums;
using Vakantiepark.Infrastructure.Persistence.Seeders;
using Xunit;

namespace Vakantiepark.UnitTests
{
    // Runs the import against the real embedded export, so a broken or changed file fails here.
    public class AccommodationSeederTests
    {
        private static readonly DateTime Now = new(2026, 11, 12, 9, 0, 0, DateTimeKind.Utc);

        private static AccommodationImport Import() => AccommodationSeeder.BuildImport(AccommodationSeeder.LoadExport(), Now);

        private static Accommodation Unit(string code) => Import().Accommodations.Single(unit => unit.Code == code);

        private static Feature FeatureByKey(string key) => Import().Features.Single(feature => feature.Key == key);

        [Fact]
        public void BuildImport_UnitCounts_MatchTheCountsInTheExport()
        {
            AccommodationExport export = AccommodationSeeder.LoadExport();

            AccommodationImport import = AccommodationSeeder.BuildImport(export, Now);

            Assert.Equal(200, import.Accommodations.Count);
            Assert.Equal(export.Counts.HolidayHomes, import.Accommodations.Count(unit => unit.Type.Category == AccommodationCategory.HolidayHome));
            Assert.Equal(export.Counts.CampingPitches, import.Accommodations.Count(unit => unit.Type.Category == AccommodationCategory.CampingPitch));
        }

        [Fact]
        public void BuildImport_Codes_AreUnique()
        {
            List<Accommodation> units = Import().Accommodations;

            Assert.Equal(units.Count, units.Select(unit => unit.Code).Distinct().Count());
        }

        [Fact]
        public void BuildImport_Types_AreTheTwelveExportTypesWithReadableSlugs()
        {
            List<AccommodationType> types = Import().Types;

            Assert.Equal(12, types.Count);
            AccommodationType mindervalide = types.Single(type => type.Name == "Wilg 4 Mindervalide");
            Assert.Equal("wilg-4-mindervalide", mindervalide.Slug);
            Assert.Equal(AccommodationCategory.HolidayHome, mindervalide.Category);
            Assert.Equal("camperplaats-xl", types.Single(type => type.Name == "Camperplaats XL").Slug);
        }

        [Fact]
        public void BuildImport_EveryUnit_LiesInAZoneOfItsOwnCategory()
        {
            AccommodationImport import = Import();

            Assert.All(import.Accommodations, unit => Assert.Equal(unit.Type.Category, unit.Zone.Category));
            Assert.Equal(import.Zones.Count, import.Zones.Select(zone => (zone.Name, zone.Category)).Distinct().Count());
        }

        [Fact]
        public void BuildImport_FirstBerk_HasTheValuesFromTheExport()
        {
            Accommodation unit = Unit("BK4-001");

            Assert.Equal("Berk 4 01", unit.Name);
            Assert.Equal("Rustweide", unit.Zone.Name);
            Assert.Equal(4, unit.MaxPersons);
            Assert.Equal(2m, unit.GetNumber(FeatureKeys.Bedrooms));
            Assert.Equal(1m, unit.GetNumber("bathrooms"));
            Assert.Equal(2m, unit.GetNumber(FeatureKeys.MaxPets));
            Assert.True(unit.HasFeature("dishwasher"));
            Assert.True(unit.HasFeature("waterfront"));
            Assert.False(unit.HasFeature("sauna"));
            Assert.False(unit.HasFeature(FeatureKeys.ElectricityAmps));
        }

        [Fact]
        public void BuildImport_CaravanPitch_HasItsNumberAndOptionValues()
        {
            Accommodation unit = Unit("CV-001");

            Assert.Equal(16m, unit.GetNumber(FeatureKeys.ElectricityAmps));
            Assert.Equal(8.0m, unit.GetNumber(FeatureKeys.MaxCaravanLengthMeters));
            Assert.Equal("reinforced_grass", unit.GetOption("surface"));
            Assert.Equal("medium", unit.GetOption("shade"));
            Assert.True(unit.HasFeature("waterConnection"));
            Assert.False(unit.HasFeature(FeatureKeys.Bedrooms));
        }

        [Fact]
        public void BuildImport_ValueType_FollowsTheExportValues()
        {
            Assert.Equal(FeatureValueType.Boolean, FeatureByKey("sauna").ValueType);
            Assert.Equal(FeatureValueType.Number, FeatureByKey(FeatureKeys.Bedrooms).ValueType);
            Assert.Equal(FeatureValueType.Number, FeatureByKey(FeatureKeys.MaxVehicleLengthMeters).ValueType);
            Assert.Equal(FeatureValueType.Option, FeatureByKey("surface").ValueType);
        }

        [Fact]
        public void BuildImport_OnlyYesAnswers_AreStoredForBooleanFeatures()
        {
            Assert.All(Import().Accommodations, unit =>
                Assert.All(unit.Features.Where(value => value.Feature.ValueType == FeatureValueType.Boolean), value =>
                {
                    Assert.Null(value.NumberValue);
                    Assert.Null(value.Option);
                }));
        }

        [Fact]
        public void BuildImport_OptionFeature_HasItsChoicesInDropdownOrder()
        {
            Feature shade = FeatureByKey("shade");

            Assert.Equal(new[] { "none", "low", "medium", "high" }, shade.Options.OrderBy(option => option.SortOrder).Select(option => option.Value));
            Assert.Equal("Gemiddeld", shade.Options.Single(option => option.Value == "medium").Name);
        }

        [Fact]
        public void BuildImport_OptionValues_PointToAnOptionOfTheirOwnFeature()
        {
            Assert.All(Import().Accommodations, unit =>
                Assert.All(unit.Features.Where(value => value.Feature.ValueType == FeatureValueType.Option), value =>
                {
                    Assert.NotNull(value.Option);
                    Assert.Contains(value.Option, value.Feature.Options);
                }));
        }

        [Fact]
        public void BuildImport_OnlyOptionFeatures_HaveOptions()
        {
            Assert.All(Import().Features.Where(feature => feature.ValueType != FeatureValueType.Option), feature => Assert.Empty(feature.Options));
        }

        [Fact]
        public void BuildImport_EveryUnit_HasAMaxPetsValue()
        {
            Assert.All(Import().Accommodations, unit => Assert.NotNull(unit.GetNumber(FeatureKeys.MaxPets)));
        }

        [Fact]
        public void BuildImport_KeysUsedByBusinessRules_ExistInTheExport()
        {
            List<string> keys = Import().Features.Select(feature => feature.Key).ToList();

            Assert.Contains(FeatureKeys.MaxPets, keys);
            Assert.Contains(FeatureKeys.Bedrooms, keys);
            Assert.Contains(FeatureKeys.WheelchairAccessible, keys);
            Assert.Contains(FeatureKeys.ElectricityAmps, keys);
            Assert.Contains(FeatureKeys.MaxCaravanLengthMeters, keys);
            Assert.Contains(FeatureKeys.MaxVehicleLengthMeters, keys);
        }

        [Fact]
        public void BuildImport_PropertiesThatRepeatAnother_AreNotFeatures()
        {
            List<Feature> features = Import().Features;

            Assert.DoesNotContain(features, feature => feature.Key is "petsAllowed" or "electricity");
        }

        [Fact]
        public void BuildImport_EveryFeature_HasADutchName()
        {
            Assert.All(Import().Features, feature => Assert.NotEqual(feature.Key, feature.Name));
        }

        [Fact]
        public void BuildImport_FeatureCategory_MatchesTheUnitsThatUseIt()
        {
            AccommodationImport import = Import();

            Assert.Equal(AccommodationCategory.HolidayHome, import.Features.Single(feature => feature.Key == "sauna").Category);
            Assert.Equal(AccommodationCategory.CampingPitch, import.Features.Single(feature => feature.Key == "carAtPitch").Category);
            Assert.Null(import.Features.Single(feature => feature.Key == FeatureKeys.MaxPets).Category);
            Assert.All(import.Accommodations, unit =>
                Assert.All(unit.Features, value => Assert.True(value.Feature.Category is null || value.Feature.Category == unit.Type.Category)));
        }

        [Fact]
        public void BuildImport_CountMismatchInExport_Throws()
        {
            AccommodationExport export = AccommodationSeeder.LoadExport();
            export.Counts.Total++;

            Assert.Throws<InvalidOperationException>(() => AccommodationSeeder.BuildImport(export, Now));
        }
    }
}
