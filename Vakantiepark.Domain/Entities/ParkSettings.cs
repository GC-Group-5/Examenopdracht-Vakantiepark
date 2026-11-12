using Vakantiepark.Domain.Interfaces;

namespace Vakantiepark.Domain.Entities
{
    // Park-wide rules the beheerder can change (US-18). There is exactly one row.
    // The tourist tax has its own table because every rate has a start date (TouristTaxRate).
    public class ParkSettings : IEntity
    {
        public Guid Id { get; set; }
        public TimeOnly ArrivalTime { get; set; }
        public TimeOnly DepartureTime { get; set; }
        public int HighSeasonMinimumNights { get; set; }

        // Part of the total paid at booking, e.g. 20 (%).
        public decimal DepositPercentage { get; set; }

        // Cancelling more than this many days before arrival is free.
        public int FreeCancellationDays { get; set; }

        // Cancelling fewer than this many days before arrival costs the full stay
        // (without tourist tax). In between, the deposit is lost.
        public int FullChargeCancellationDays { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
