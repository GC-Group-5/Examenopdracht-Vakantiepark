using Vakantiepark.Domain.Enums;
using Vakantiepark.Domain.Interfaces;

namespace Vakantiepark.Domain.Entities
{
    // A property an accommodation can have, e.g. 'sauna' (yes/no), 'bedrooms' (number)
    // or 'surface' (one of a fixed list of options). The value per accommodation lives in AccommodationFeature.
    public class Feature : IEntity
    {
        public Guid Id { get; set; }

        // Stable key used in code and filters; the property name from the export, e.g. 'hotTub'.
        // Keys that business rules use are in FeatureKeys.
        public string Key { get; set; } = string.Empty;

        // Dutch name shown to guests, e.g. 'Hottub'.
        public string Name { get; set; } = string.Empty;

        public FeatureValueType ValueType { get; set; }

        // Null = applies to holiday homes and camping pitches.
        public AccommodationCategory? Category { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        // Navigation Properties
        public List<FeatureOption> Options { get; set; } = [];
        public List<AccommodationFeature> Accommodations { get; set; } = [];
    }
}
