using Vakantiepark.Domain.Interfaces;

namespace Vakantiepark.Domain.Entities
{
    // Set by the municipality and changed now and then. A new rate is a new row, so
    // existing bookings keep the rate of their nights. The rate for a night is the one
    // with the latest ValidFrom on or before that night.
    public class TouristTaxRate : IEntity
    {
        public Guid Id { get; set; }
        public decimal AmountPerPersonPerNight { get; set; }
        public DateOnly ValidFrom { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
