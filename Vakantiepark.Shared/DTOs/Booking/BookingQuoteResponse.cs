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
        public int NumberOfChildrenUnder12 { get; set; }
        public int MaxPersons { get; set; }
        public int MaxPets { get; set; }
        public PriceSpecificationResponse Price { get; set; } = new();

        // Cancellation terms (US-12): free before this many days, no refund within the other.
        public int FreeCancellationDaysBeforeArrival { get; set; }
        public int NoRefundDaysBeforeArrival { get; set; }
        public decimal DepositPercentage { get; set; }

        // US-46: when and where the deposit must be transferred if the guest books now.
        public DateOnly DepositDueDate { get; set; }
        public string BankAccountNumber { get; set; } = string.Empty;
        public string BankAccountName { get; set; } = string.Empty;
    }
}
