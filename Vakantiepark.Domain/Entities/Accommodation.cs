using Vakantiepark.Domain.Interfaces;

namespace Vakantiepark.Domain.Entities
{
    // One physical unit on the park (a house or a pitch). Everything the client's export
    // lists under 'properties' (bedrooms, sauna, surface, max pets...) is a feature with a
    // value (AccommodationFeature), so a new property never needs a new column.
    public class Accommodation : IEntity
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int MaxPersons { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        // Navigation Keys
        public Guid TypeId { get; set; }
        public Guid ZoneId { get; set; }

        // Navigation Properties
        public AccommodationType Type { get; set; } = null!;
        public Zone Zone { get; set; } = null!;
        public List<AccommodationFeature> Features { get; set; } = [];

        // Helpers for business rules; they need Features (with Feature) to be loaded.
        public bool HasFeature(string key) => Features.Any(feature => feature.Feature.Key == key);

        public decimal? GetNumber(string key) => Features.FirstOrDefault(feature => feature.Feature.Key == key)?.NumberValue;

        // The chosen option's value, e.g. 'reinforced_grass'; needs Option to be loaded as well.
        public string? GetOption(string key) => Features.FirstOrDefault(feature => feature.Feature.Key == key)?.Option?.Value;
    }
}
