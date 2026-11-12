using Vakantiepark.Domain.Interfaces;

namespace Vakantiepark.Domain.Entities
{
    // A period with its own prices, e.g. 'Zomervakantie 2027'. The park sets its own
    // periods every year; nights outside any period use the type's default price.
    // Start and end date are both nights of the period.
    public class Season : IEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }

        // The minimum stay from the park settings applies to bookings that touch this period.
        public bool IsHighSeason { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        // Navigation Properties
        public List<AccommodationTypePrice> Prices { get; set; } = [];
    }
}
