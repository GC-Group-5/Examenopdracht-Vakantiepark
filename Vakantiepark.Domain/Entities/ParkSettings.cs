using Vakantiepark.Domain.Interfaces;

namespace Vakantiepark.Domain.Entities
{
    public class ParkSettings : IEntity
    {
        public Guid Id { get; set; }
        public TimeOnly ArrivalTime { get; set; }
        public TimeOnly DepartureTime { get; set; }
        public decimal TouristTaxPerPersonPerNight { get; set; }
        public int HighSeasonMinimumNights { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
