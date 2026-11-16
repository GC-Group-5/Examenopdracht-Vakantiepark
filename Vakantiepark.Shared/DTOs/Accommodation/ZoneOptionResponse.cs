namespace Vakantiepark.Shared.DTOs.Accommodation
{
    // Zone choice for the search form (US-06): zones belong to one category,
    // the form only offers the zones matching the selected type.
    public class ZoneOptionResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;

        // HolidayHome or CampingPitch.
        public string Category { get; set; } = string.Empty;
    }
}
