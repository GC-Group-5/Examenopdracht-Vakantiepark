using Vakantiepark.Domain.Interfaces;

namespace Vakantiepark.Domain.Entities
{
    public class Season : IEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        // Navigation Properties
        public List<AccommodationTypePrice> Prices { get; set; } = [];
    }
}
