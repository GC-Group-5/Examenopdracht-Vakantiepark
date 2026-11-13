using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Vakantiepark.Domain.Entities;
using Vakantiepark.Domain.Enums;

namespace Vakantiepark.Infrastructure.Persistence.Seeders
{
    // Imports the client's export (Seeders/Data/accommodaties.json, embedded in this
    // assembly) into an empty database: types, zones, features and the 200 units.
    // Every entry under 'properties' becomes a feature with a value. Once the units exist
    // the database is leading; the beheerder edits them there and the import never
    // overwrites that.
    public static class AccommodationSeeder
    {
        private const string ExportResource = "accommodaties.json";

        // Properties that only repeat another one: petsAllowed = maxPets > 0,
        // electricity = electricityAmps > 0. Storing both lets them contradict each other.
        private static readonly HashSet<string> DerivedProperties = ["petsAllowed", "electricity"];

        // Dutch name per property. An unknown property stops the import, so a feature
        // never reaches the website without a name.
        private static readonly Dictionary<string, string> FeatureNames = new()
        {
            ["bedrooms"] = "Slaapkamers",
            ["bathrooms"] = "Badkamers",
            ["beds"] = "Bedden",
            ["maxPets"] = "Maximaal aantal huisdieren",
            ["adaptedBathroom"] = "Aangepaste badkamer",
            ["adjustableBed"] = "Verstelbaar bed",
            ["airConditioning"] = "Airco",
            ["chargingPoint"] = "Laadpaal",
            ["childFriendly"] = "Kindvriendelijk",
            ["coveredTerrace"] = "Overdekt terras",
            ["dishwasher"] = "Vaatwasser",
            ["extraParkingSpace"] = "Extra parkeerplaats",
            ["fireplace"] = "Open haard",
            ["hotTub"] = "Hottub",
            ["levelAccess"] = "Drempelvrij",
            ["nearPool"] = "Dicht bij het zwembad",
            ["sauna"] = "Sauna",
            ["smokeFree"] = "Rookvrij",
            ["washingMachine"] = "Wasmachine",
            ["waterfront"] = "Aan het water",
            ["wheelchairAccessible"] = "Rolstoeltoegankelijk",
            ["widenedDoors"] = "Verbrede deuren",
            ["electricityAmps"] = "Stroom (ampère)",
            ["surface"] = "Ondergrond",
            ["shade"] = "Schaduw",
            ["wifiCoverage"] = "Wifi-bereik",
            ["maxCaravanLengthMeters"] = "Maximale caravanlengte (m)",
            ["maxVehicleLengthMeters"] = "Maximale voertuiglengte (m)",
            ["carAtPitch"] = "Auto bij de plaats",
            ["drainConnection"] = "Afvoeraansluiting",
            ["extraWide"] = "Extra brede plaats",
            ["nearSanitary"] = "Dicht bij het sanitair",
            ["privateSanitary"] = "Eigen sanitair",
            ["waterConnection"] = "Wateraansluiting",
        };

        // Choices per option feature, in dropdown order. A value in the export that is not
        // listed here stops the import.
        private static readonly Dictionary<string, (string Value, string Name)[]> OptionNames = new()
        {
            ["surface"] = [("grass", "Gras"), ("reinforced_grass", "Versterkt gras"), ("paved", "Verhard")],
            ["shade"] = [("none", "Geen"), ("low", "Weinig"), ("medium", "Gemiddeld"), ("high", "Veel")],
            ["wifiCoverage"] = [("limited", "Beperkt"), ("standard", "Standaard"), ("good", "Goed")],
        };

        // Intro text per type, copied word for word from the website template
        // (accommodaties/*.html and verblijven.html). The export only lists units, so the
        // type texts are not in it. An unknown type stops the import.
        public static readonly IReadOnlyDictionary<string, string> TypeDescriptions = new Dictionary<string, string>
        {
            ["Berk 4"] = "Compact en sfeervol voor een ontspannen verblijf met z’n tweeën of met het gezin.",
            ["Berk 6"] = "Een fijne familiewoning met extra leefruimte, midden in het groen.",
            ["Eik 6"] = "Comfortabel en eigentijds, met ruimte om samen te koken, eten en ontspannen.",
            ["Eik Luxe 6"] = "Onze luxe zespersoons variant met extra comfort en op veel woningen wellnessvoorzieningen.",
            ["Beuk 8"] = "Ruim genoeg voor twee gezinnen of een vriendengroep, zonder dat het massaal aanvoelt.",
            ["Wilg 4 Mindervalide"] = "Gelijkvloers en toegankelijk ingericht, met extra aandacht voor bewegingsruimte en comfort.",
            ["Bosvilla 10"] = "Een royale villa voor grotere gezelschappen, rustig gelegen en met veel binnen- en buitenruimte.",
            ["Tentplaats Basis"] = "Een groene kampeerplek voor wie graag eenvoudig en dicht bij de natuur verblijft.",
            ["Tentplaats Comfort"] = "Kamperen met nét wat meer gemak, zoals stroom en op veel plaatsen een wateraansluiting.",
            ["Caravanplaats"] = "Een ruime plaats met voorzieningen voor caravan en auto, omgeven door groen.",
            ["Camperplaats"] = "Een verharde camperplaats met praktische aansluitingen voor een comfortabel verblijf.",
            ["Camperplaats XL"] = "Extra breed en extra ruim voor grotere campers, met uitgebreide aansluitmogelijkheden.",
        };

        // The export uses camelCase names and snake_case categories ('holiday_home').
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) },
        };

        public static async Task SeedAccommodationsAsync(this ApplicationDBContext context, CancellationToken cancellationToken = default)
        {
            if (await context.Accommodations.AnyAsync(cancellationToken))
            {
                // Databases imported before descriptions existed get them once; a text the
                // beheerder has written is never overwritten.
                List<AccommodationType> withoutDescription = await context.AccommodationTypes
                    .Where(type => type.Description == string.Empty)
                    .ToListAsync(cancellationToken);

                foreach (AccommodationType type in withoutDescription)
                    type.Description = DescriptionOf(type.Name);

                await context.SaveChangesAsync(cancellationToken);
                return;
            }

            AccommodationImport import = BuildImport(LoadExport(), DateTime.UtcNow);

            context.AccommodationTypes.AddRange(import.Types);
            context.Zones.AddRange(import.Zones);
            context.Features.AddRange(import.Features);
            context.Accommodations.AddRange(import.Accommodations);
            await context.SaveChangesAsync(cancellationToken);
        }

        private static string DescriptionOf(string typeName) =>
            TypeDescriptions.TryGetValue(typeName, out string? description)
                ? description
                : throw new InvalidOperationException($"Voor type '{typeName}' is geen omschrijving in de seeder.");

        public static AccommodationExport LoadExport()
        {
            using Stream stream = typeof(AccommodationSeeder).Assembly.GetManifestResourceStream(ExportResource)
                ?? throw new InvalidOperationException($"Het exportbestand '{ExportResource}' is niet in de assembly opgenomen.");

            return JsonSerializer.Deserialize<AccommodationExport>(stream, JsonOptions)
                ?? throw new InvalidOperationException($"Het exportbestand '{ExportResource}' is leeg.");
        }

        public static AccommodationImport BuildImport(AccommodationExport export, DateTime now)
        {
            if (export.Accommodations.Count != export.Counts.Total)
                throw new InvalidOperationException($"De export bevat {export.Accommodations.Count} accommodaties, maar meldt er {export.Counts.Total}.");

            Dictionary<string, AccommodationType> types = export.Accommodations
                .GroupBy(unit => unit.Type)
                .ToDictionary(group => group.Key, group => new AccommodationType
                {
                    Id = Guid.NewGuid(),
                    Slug = group.Key.ToLowerInvariant().Replace(' ', '-'),
                    Name = group.Key,
                    Category = group.First().Category,
                    Description = DescriptionOf(group.Key),
                    CreatedAt = now,
                });

            Dictionary<(string Zone, AccommodationCategory Category), Zone> zones = export.Accommodations
                .Select(unit => (unit.Zone, unit.Category))
                .Distinct()
                .ToDictionary(key => key, key => new Zone { Id = Guid.NewGuid(), Name = key.Zone, Category = key.Category, CreatedAt = now });

            Dictionary<string, Feature> features = export.Accommodations
                .SelectMany(unit => Properties(unit).Select(property => (property.Key, property.Value.ValueKind, unit.Category)))
                .GroupBy(property => property.Key)
                .ToDictionary(group => group.Key, group => BuildFeature(
                    group.Key,
                    ValueTypeOf(group.Key, group.Select(property => property.ValueKind)),
                    group.Select(property => property.Category).Distinct().ToList(),
                    now));

            List<Accommodation> accommodations = export.Accommodations.Select(unit => new Accommodation
            {
                Id = Guid.NewGuid(),
                Code = unit.Code,
                Name = unit.Name,
                MaxPersons = unit.MaxPersons,
                Type = types[unit.Type],
                Zone = zones[(unit.Zone, unit.Category)],
                // A 'no' is stored as a missing row; numbers and options are always stored.
                Features = Properties(unit)
                    .Where(property => property.Value.ValueKind != JsonValueKind.False)
                    .Select(property => ToValue(features[property.Key], property.Value, unit.Code))
                    .ToList(),
                CreatedAt = now,
            }).ToList();

            return new AccommodationImport(types.Values.ToList(), zones.Values.ToList(), features.Values.ToList(), accommodations);
        }

        private static Feature BuildFeature(string key, FeatureValueType valueType, List<AccommodationCategory> categories, DateTime now)
        {
            if (!FeatureNames.TryGetValue(key, out string? name))
                throw new InvalidOperationException($"De eigenschap '{key}' uit de export heeft nog geen Nederlandse naam in de seeder.");

            List<FeatureOption> options = [];
            if (valueType == FeatureValueType.Option)
            {
                if (!OptionNames.TryGetValue(key, out (string Value, string Name)[]? choices))
                    throw new InvalidOperationException($"De eigenschap '{key}' uit de export heeft nog geen keuzelijst in de seeder.");

                options = choices.Select((choice, index) => new FeatureOption
                {
                    Id = Guid.NewGuid(),
                    Value = choice.Value,
                    Name = choice.Name,
                    SortOrder = index,
                    CreatedAt = now,
                }).ToList();
            }

            return new Feature
            {
                Id = Guid.NewGuid(),
                Key = key,
                Name = name,
                ValueType = valueType,
                Category = categories.Count == 1 ? categories[0] : null,
                Options = options,
                CreatedAt = now,
            };
        }

        private static AccommodationFeature ToValue(Feature feature, JsonElement value, string code) => new()
        {
            Feature = feature,
            NumberValue = value.ValueKind == JsonValueKind.Number ? value.GetDecimal() : null,
            Option = value.ValueKind == JsonValueKind.String
                ? feature.Options.SingleOrDefault(option => option.Value == value.GetString())
                    ?? throw new InvalidOperationException($"De waarde '{value.GetString()}' van '{feature.Key}' bij {code} staat niet in de keuzelijst.")
                : null,
        };

        private static IEnumerable<KeyValuePair<string, JsonElement>> Properties(ExportAccommodation unit) =>
            unit.Properties.Where(property => !DerivedProperties.Contains(property.Key) && property.Value.ValueKind != JsonValueKind.Null);

        // A property must have the same kind of value on every unit.
        private static FeatureValueType ValueTypeOf(string key, IEnumerable<JsonValueKind> kinds)
        {
            List<FeatureValueType> valueTypes = kinds.Select(kind => kind switch
            {
                JsonValueKind.True or JsonValueKind.False => FeatureValueType.Boolean,
                JsonValueKind.Number => FeatureValueType.Number,
                JsonValueKind.String => FeatureValueType.Option,
                _ => throw new InvalidOperationException($"De eigenschap '{key}' heeft een waarde die geen ja/nee, getal of keuze is."),
            }).Distinct().ToList();

            return valueTypes.Count == 1
                ? valueTypes[0]
                : throw new InvalidOperationException($"De eigenschap '{key}' heeft niet bij iedere accommodatie hetzelfde soort waarde.");
        }
    }
}
