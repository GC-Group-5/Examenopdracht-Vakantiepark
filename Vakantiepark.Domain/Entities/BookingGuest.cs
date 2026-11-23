namespace Vakantiepark.Domain.Entities
{
    // One person in the travel party; the main booker is one of them (IsMainBooker).
    // A fellow traveller's birth date may be missing: the guest can leave it open and
    // reception completes it at check-in.
    public class BookingGuest
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateOnly? BirthDate { get; set; }
        public bool IsMainBooker { get; set; }

        // Navigation Keys
        public Guid BookingId { get; set; }

        // Navigation Properties
        public Booking Booking { get; set; } = null!;
    }
}
