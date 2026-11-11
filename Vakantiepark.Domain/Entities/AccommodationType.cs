using Vakantiepark.Domain.Enums;
using Vakantiepark.Domain.Interfaces;

namespace Vakantiepark.Domain.Entities
{
    public class AccommodationType : IEntity
    {
        public Guid Id { get; set; }
        public string Slug { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public AccommodationCategory Category { get; set; }

        // Final cleaning is charged once per booking; the amount is a per-type setting (US-05).
        public decimal CleaningFee { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        // Navigation Properties
        public List<Accommodation> Accommodations { get; set; } = [];
        public List<AccommodationTypePrice> Prices { get; set; } = [];
    }
}
