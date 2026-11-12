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

        // Cancellation rules (US-12):
        // - more than FreeCancellationDaysBeforeArrival days before arrival: everything is refunded;
        // - fewer than NoRefundDaysBeforeArrival days: the stay is not refunded, the tourist tax is;
        // - in between: the deposit is lost, the rest is refunded.
        public int FreeCancellationDaysBeforeArrival { get; set; }
        public int NoRefundDaysBeforeArrival { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
