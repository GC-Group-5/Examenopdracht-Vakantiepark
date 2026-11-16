namespace Vakantiepark.Domain.Entities
{
    // One person in the travel party. Name and date of birth are needed for the
    // tourist tax; the main booker is one of them (IsMainBooker).
    public class BookingGuest
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateOnly BirthDate { get; set; }
        public bool IsMainBooker { get; set; }

        // Navigation Keys
        public Guid BookingId { get; set; }

        // Navigation Properties
        public Booking Booking { get; set; } = null!;
    }
}
