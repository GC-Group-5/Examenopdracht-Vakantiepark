namespace Vakantiepark.Domain.Entities
{
    // A fellow traveler of the main booker. Name and birth date are needed for the tourist tax.
    public class BookingTraveler
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateOnly BirthDate { get; set; }

        // Navigation Keys
        public Guid BookingId { get; set; }
    }
}
