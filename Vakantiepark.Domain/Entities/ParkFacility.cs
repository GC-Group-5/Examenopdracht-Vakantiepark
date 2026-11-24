using Vakantiepark.Domain.Enums;
using Vakantiepark.Domain.Interfaces;

namespace Vakantiepark.Domain.Entities
{
    // Park information for guests (US-20): a restaurant, the shop, a facility or an activity.
    // Facilities have the same opening hours every day; an activity has one start and end instead.
    public class ParkFacility : IEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ParkFacilityCategory Category { get; set; }
        public string Location { get; set; } = string.Empty;
        public string PhotoUrl { get; set; } = string.Empty;

        // Park time (Europe/Amsterdam).
        public TimeOnly? OpensAt { get; set; }
        public TimeOnly? ClosesAt { get; set; }

        // UTC, only for activities.
        public DateTime? StartsAt { get; set; }
        public DateTime? EndsAt { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
