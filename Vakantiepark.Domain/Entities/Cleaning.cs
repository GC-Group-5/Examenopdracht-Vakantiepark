using Vakantiepark.Domain.Enums;
using Vakantiepark.Domain.Interfaces;

namespace Vakantiepark.Domain.Entities
{
    // US-32: one cleaning per departure. Together the rows are the history of who cleaned which unit and when.
    public class Cleaning : IEntity
    {
        public Guid Id { get; set; }
        public CleaningStatus Status { get; set; } = CleaningStatus.ToClean;

        // Set when a cleaner starts, and again when he finishes.
        public DateTime? StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        // Navigation Keys
        public Guid BookingId { get; set; }
        public Guid AccommodationUnitId { get; set; }

        // The cleaner who is busy or who finished; empty while nobody has started.
        public Guid? UserId { get; set; }

        // Navigation Properties
        public Booking Booking { get; set; } = null!;
        public Accommodation AccommodationUnit { get; set; } = null!;
        public User? User { get; set; }
    }
}
