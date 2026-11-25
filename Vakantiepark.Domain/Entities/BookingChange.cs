namespace Vakantiepark.Domain.Entities
{
    // US-14: every change or cancellation by staff, so the detail page shows who did what and when.
    public class BookingChange
    {
        public Guid Id { get; set; }
        public DateTime ChangedAt { get; set; }
        public string Description { get; set; } = string.Empty;

        // Only for a cancellation, where staff must give a reason.
        public string? Reason { get; set; }

        // Navigation Keys
        public Guid BookingId { get; set; }
        public Guid UserId { get; set; }

        // Navigation Properties
        public Booking Booking { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}
