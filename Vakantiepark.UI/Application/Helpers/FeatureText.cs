using Vakantiepark.Shared.DTOs.Accommodation;

namespace Vakantiepark.UI.Application.Helpers
{
    public static class FeatureText
    {
        public static string Format(AccommodationFeatureResponse feature)
            => feature.Key switch
            {
                "bedrooms" when feature.NumberValue is decimal bedrooms
                    => $"{bedrooms:0} {(bedrooms == 1 ? "slaapkamer" : "slaapkamers")}",
                "maxPets" when feature.NumberValue is 0
                    => "Geen huisdieren toegestaan",
                "maxPets" when feature.NumberValue is decimal maxPets
                    => $"Maximaal {maxPets:0} {(maxPets == 1 ? "huisdier" : "huisdieren")}",
                _ when feature.NumberValue is decimal number => $"{number:0.##} {feature.Name}",
                _ => feature.OptionName ?? feature.Name,
            };
    }
}
