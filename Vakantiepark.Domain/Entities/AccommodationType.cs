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

        // Short intro text shown on the overview and the detail page (taken from the template).
        public string Description { get; set; } = string.Empty;

        // Applies to every night that falls outside a season with its own price.
        public decimal DefaultPricePerNight { get; set; }

        // Charged once per booking.
        public decimal CleaningFee { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        // Navigation Properties
        public List<Accommodation> Accommodations { get; set; } = [];
        public List<AccommodationTypePrice> Prices { get; set; } = [];
    }
}
