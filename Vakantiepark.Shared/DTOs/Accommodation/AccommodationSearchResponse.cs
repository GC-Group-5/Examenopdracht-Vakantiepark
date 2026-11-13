namespace Vakantiepark.Shared.DTOs.Accommodation
{
    public class AccommodationSearchResponse
    {
        public List<AccommodationTypeAvailability> Results { get; set; } = [];

        // Dutch guidance shown to the visitor when there are no results; null when results are available.
        public string? Message { get; set; }
    }

    public class AccommodationTypeAvailability
    {
        public string Slug { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int MaxPersons { get; set; }
        public int AvailableUnitCount { get; set; }

        // Total price of the whole stay for one unit, calculated by the price service.
        public decimal TotalPrice { get; set; }

        public List<ZoneAvailability> Zones { get; set; } = [];
    }

    public class ZoneAvailability
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int AvailableUnitCount { get; set; }
    }
}
