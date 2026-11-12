namespace Vakantiepark.Shared.DTOs.Accommodation
{
    // One feature of an accommodation. Only the value that fits ValueType is filled:
    // nothing for Boolean (present = yes), NumberValue for Number, Option* for Option.
    public class AccommodationFeatureResponse
    {
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ValueType { get; set; } = string.Empty;
        public decimal? NumberValue { get; set; }
        public string? OptionValue { get; set; }
        public string? OptionName { get; set; }
    }
}
