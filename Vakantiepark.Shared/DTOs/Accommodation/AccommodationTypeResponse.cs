namespace Vakantiepark.Shared.DTOs.Accommodation
{
    // One accommodation type with everything the overview and the detail page show,
    // combined over all units of that type.
    public class AccommodationTypeResponse
    {
        public string Slug { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int MaxPersons { get; set; }
        public int UnitCount { get; set; }

        // Zones where units of this type are, alphabetical.
        public List<string> Zones { get; set; } = [];

        // Short feature texts as the website template shows them, e.g. "2 slaapkamers" or "Sauna mogelijk".
        public List<string> Highlights { get; set; } = [];

        public List<TypeFeatureResponse> Features { get; set; } = [];
    }

    public class TypeFeatureResponse
    {
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ValueType { get; set; } = string.Empty;
        public int UnitCount { get; set; }
        public decimal? MinNumber { get; set; }
        public decimal? MaxNumber { get; set; }
        public List<string> OptionNames { get; set; } = [];
    }
}
