using Vakantiepark.Shared.DTOs.Price;

namespace Vakantiepark.Shared.DTOs.Booking
{
    // Everything the summary shows before the guest confirms: the stay, the price and the
    // park rules that apply. Calculated by the server, never by the browser.
    public class BookingQuoteResponse
    {
        public string AccommodationTypeName { get; set; } = string.Empty;
        public string? Zone { get; set; }
        public DateOnly ArrivalDate { get; set; }
        public DateOnly DepartureDate { get; set; }
        public TimeOnly ArrivalTime { get; set; }
        public TimeOnly DepartureTime { get; set; }
        public int NumberOfGuests { get; set; }
        public PriceSpecificationResponse Price { get; set; } = new();

        // Cancellation terms (US-12): free before this many days, no refund within the other.
        public int FreeCancellationDaysBeforeArrival { get; set; }
        public int NoRefundDaysBeforeArrival { get; set; }
        public decimal DepositPercentage { get; set; }
    }
}
