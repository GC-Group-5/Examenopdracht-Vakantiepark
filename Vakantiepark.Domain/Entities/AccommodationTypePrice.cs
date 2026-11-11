using Vakantiepark.Domain.Interfaces;

namespace Vakantiepark.Domain.Entities
{
    public class AccommodationTypePrice : IEntity
    {
        public Guid Id { get; set; }
        public decimal PricePerNight { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        // Navigation Keys
        public Guid AccommodationTypeId { get; set; }
        public Guid SeasonId { get; set; }

        // Navigation Properties
        public AccommodationType AccommodationType { get; set; } = null!;
        public Season Season { get; set; } = null!;
    }
}
