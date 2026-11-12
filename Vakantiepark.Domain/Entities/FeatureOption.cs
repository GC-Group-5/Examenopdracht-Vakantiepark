using Vakantiepark.Domain.Interfaces;

namespace Vakantiepark.Domain.Entities
{
    // One choice of an option feature, e.g. 'Versterkt gras' for 'Ondergrond'. The beheerder
    // picks from these in a dropdown, so a value can never be mistyped.
    public class FeatureOption : IEntity
    {
        public Guid Id { get; set; }

        // Stable value used in code, e.g. 'reinforced_grass'.
        public string Value { get; set; } = string.Empty;

        // Dutch name shown in lists, e.g. 'Versterkt gras'.
        public string Name { get; set; } = string.Empty;

        public int SortOrder { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        // Navigation Keys
        public Guid FeatureId { get; set; }

        // Navigation Properties
        public Feature Feature { get; set; } = null!;
    }
}
