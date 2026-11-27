namespace Vakantiepark.Shared.DTOs.Booking
{
    public class MyBookingResponse : BookingResponse
    {
        public string AccommodationTypeName { get; set; } = string.Empty;
        public string Zone { get; set; } = string.Empty;
        public List<GuestResponse> Guests { get; set; } = [];
        public string Status { get; set; } = string.Empty;

        // US-12: what the page may offer, and the costs once the booking is cancelled.
        public bool CanCancel { get; set; }
        public bool CanChange { get; set; }
        public decimal? CancellationFee { get; set; }

        // US-46: where an unpaid deposit goes; empty on the staff detail page, which does not need it.
        public string BankAccountNumber { get; set; } = string.Empty;
        public string BankAccountName { get; set; } = string.Empty;
    }
}
