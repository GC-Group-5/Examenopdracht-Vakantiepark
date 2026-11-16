using Vakantiepark.Domain.Enums;
using Vakantiepark.Domain.Interfaces;

namespace Vakantiepark.Domain.Entities
{
    // US-17: a unit is temporarily not rentable, e.g. for maintenance or a technical
    // problem. The period blocks booking for overlapping stays; an existing booking in
    // the period is flagged so staff can act on it.
    public class MaintenancePeriod : IEntity
    {
        public Guid Id { get; set; }

        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }

        public MaintenanceReason Reason { get; set; }

        // Extra explanation shown to staff; required when Reason is Other.
        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        // Navigation Keys
        public Guid AccommodationUnitId { get; set; }

        // Navigation Properties
        public Accommodation AccommodationUnit { get; set; } = null!;
    }
}
