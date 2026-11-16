using System.Globalization;
using Vakantiepark.Domain.Constants;
using Vakantiepark.Domain.Entities;
using Vakantiepark.Domain.Enums;

namespace Vakantiepark.Application.Helpers
{
    // The feature chips of a type, in the order and wording of the website template.
    // Units of one type differ, so a feature that only some units have is still shown;
    // sauna, hot tub and the camping connections then read "mogelijk".
    public static class AccommodationTypeHighlights
    {
        public const int MaxHighlights = 7;

        private static readonly (string Key, string Text)[] HolidayHomeExtras =
        [
            (FeatureKeys.WheelchairAccessible, "Toegankelijk"),
            (FeatureKeys.Sauna, "Sauna mogelijk"),
            (FeatureKeys.HotTub, "Hottub mogelijk"),
            (FeatureKeys.Dishwasher, "Vaatwasser"),
            (FeatureKeys.WashingMachine, "Wasmachine"),
            (FeatureKeys.AirConditioning, "Airco"),
            (FeatureKeys.Fireplace, "Haard"),
            (FeatureKeys.CoveredTerrace, "Overdekt terras"),
        ];

        private static readonly (string Key, string Text)[] CampingPitchConnections =
        [
            (FeatureKeys.WaterConnection, "Wateraansluiting mogelijk"),
            (FeatureKeys.DrainConnection, "Afvoer mogelijk"),
            (FeatureKeys.PrivateSanitary, "Privé sanitair mogelijk"),
        ];

        // Needs the units with their type and their features (with Feature) loaded.
        public static List<string> For(IReadOnlyCollection<Accommodation> units)
        {
            if (units.Count == 0)
                return [];

            List<string> highlights = units.First().Type.Category == AccommodationCategory.HolidayHome
                ? ForHolidayHome(units)
                : ForCampingPitch(units);

            return highlights.Take(MaxHighlights).ToList();
        }

        private static List<string> ForHolidayHome(IReadOnlyCollection<Accommodation> units)
        {
            List<string> highlights = [];
            AddRange(highlights, units, FeatureKeys.Bedrooms, "slaapkamer", "slaapkamers");
            AddRange(highlights, units, FeatureKeys.Bathrooms, "badkamer", "badkamers");

            foreach ((string key, string text) in HolidayHomeExtras)
            {
                if (units.Any(unit => unit.HasFeature(key)))
                    highlights.Add(text);
            }

            return highlights;
        }

        private static List<string> ForCampingPitch(IReadOnlyCollection<Accommodation> units)
        {
            List<string> highlights = [];

            List<decimal> amps = Numbers(units, FeatureKeys.ElectricityAmps)
                .Where(value => value > 0)
                .Distinct()
                .Order()
                .ToList();
            if (amps.Count > 0)
                highlights.Add($"Stroom {string.Join("/", amps.Select(Format))}A");

            foreach ((string key, string text) in CampingPitchConnections)
            {
                if (units.Any(unit => unit.HasFeature(key)))
                    highlights.Add(text);
            }

            List<decimal> caravanLengths = Numbers(units, FeatureKeys.MaxCaravanLengthMeters);
            if (caravanLengths.Count > 0)
                highlights.Add($"Caravan tot {Format(caravanLengths.Max())} m");

            List<decimal> vehicleLengths = Numbers(units, FeatureKeys.MaxVehicleLengthMeters);
            if (vehicleLengths.Count > 0)
                highlights.Add($"Camper tot {Format(vehicleLengths.Max())} m");

            if (Numbers(units, FeatureKeys.MaxPets).Any(value => value > 0))
                highlights.Add("Huisdieren mogelijk");

            return highlights;
        }

        // "2 slaapkamers", "1 badkamer" or "2–3 badkamers" when the units differ.
        private static void AddRange(List<string> highlights, IReadOnlyCollection<Accommodation> units, string key, string singular, string plural)
        {
            List<decimal> values = Numbers(units, key);
            if (values.Count == 0)
                return;

            decimal min = values.Min();
            decimal max = values.Max();

            if (min == max)
                highlights.Add($"{Format(min)} {(min == 1 ? singular : plural)}");
            else
                highlights.Add($"{Format(min)}–{Format(max)} {plural}");
        }

        private static List<decimal> Numbers(IReadOnlyCollection<Accommodation> units, string key)
            => units
                .Select(unit => unit.GetNumber(key))
                .Where(value => value is not null)
                .Select(value => value!.Value)
                .ToList();

        // The template writes "8.5 m", so a dot regardless of the server's culture.
        private static string Format(decimal value) => value.ToString("0.#", CultureInfo.InvariantCulture);
    }
}
