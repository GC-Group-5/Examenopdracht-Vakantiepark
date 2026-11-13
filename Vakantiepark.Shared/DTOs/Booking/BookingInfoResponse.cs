namespace Vakantiepark.Shared.DTOs.Booking
{
    // Everything the booking form shows next to the guest's own input: the stay and the park rules.
    public class BookingInfoResponse
    {
        public string AccommodationTypeName { get; set; } = string.Empty;
        public List<string> Zones { get; set; } = [];
        public TimeOnly ArrivalTime { get; set; }
        public TimeOnly DepartureTime { get; set; }
        public decimal DepositPercentage { get; set; }
        public int FreeCancellationDaysBeforeArrival { get; set; }
        public int NoRefundDaysBeforeArrival { get; set; }
    }
}
