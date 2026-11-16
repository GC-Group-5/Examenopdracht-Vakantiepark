namespace Vakantiepark.Shared.DTOs.Booking
{
    public class MyBookingResponse : BookingResponse
    {
        public string AccommodationTypeName { get; set; } = string.Empty;
        public List<GuestResponse> Guests { get; set; } = [];
        public string Status { get; set; } = string.Empty;

        public bool CanCancel { get; set; }
        public bool CanChange { get; set; }
        public decimal? CancellationFee { get; set; }
    }
}
