using Vakantiepark.Domain.Enums;
using Vakantiepark.Domain.Interfaces;

namespace Vakantiepark.Domain.Entities
{
    // One row per yes/no feature (vlag): adding a feature is one data row,
    // no property, no column, no migration. Key is the stable machine key
    // used in code and filters ("sauna"); Name is the Dutch name shown to
    // guests. Category null = applies to homes and pitches.
    public class Feature : IEntity
    {
        public Guid Id { get; set; }

        // Stable machine key, kebab-case, e.g. 'sauna' or 'car-at-pitch'.
        public string Key { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        // null = both categories; a pitch-only flag never shows on a home's chip list.
        public AccommodationCategory? Category { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        // Navigation Properties
        public List<Accommodation> Accommodations { get; set; } = [];
    }
}
