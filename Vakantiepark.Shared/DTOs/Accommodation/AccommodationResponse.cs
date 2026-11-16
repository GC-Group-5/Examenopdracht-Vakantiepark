namespace Vakantiepark.Shared.DTOs.Accommodation
{
    public class AccommodationResponse
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty;
        public string TypeSlug { get; set; } = string.Empty;
        public string TypeDescription { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Zone { get; set; } = string.Empty;
        public int MaxPersons { get; set; }
        public List<AccommodationFeatureResponse> Features { get; set; } = [];
    }
}
