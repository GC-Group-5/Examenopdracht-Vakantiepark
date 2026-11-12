namespace Vakantiepark.Shared.DTOs.Accommodation
{
    // A feature as the beheerder's edit form needs it: ValueType picks the control
    // (checkbox, number field or dropdown) and Options fills the dropdown.
    public class FeatureResponse
    {
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ValueType { get; set; } = string.Empty;
        public string? Category { get; set; }
        public List<FeatureOptionResponse> Options { get; set; } = [];
    }

    public class FeatureOptionResponse
    {
        public string Value { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
