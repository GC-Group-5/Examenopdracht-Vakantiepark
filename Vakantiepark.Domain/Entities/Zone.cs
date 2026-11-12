using Vakantiepark.Domain.Enums;
using Vakantiepark.Domain.Interfaces;

namespace Vakantiepark.Domain.Entities
{
    // The physical location of a unit on the park, e.g. 'Waterkant' or 'Bosrand'.
    // A zone belongs to either the holiday homes or the camping, never both, so a full
    // waterfront camping never blocks a guest who wants a waterfront house.
    public class Zone : IEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public AccommodationCategory Category { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        // Navigation Properties
        public List<Accommodation> Accommodations { get; set; } = [];
    }
}
