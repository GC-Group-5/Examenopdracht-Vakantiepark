namespace Vakantiepark.Shared.DTOs.Booking
{
    public class BookingAccessRequest
    {
        public string? Token { get; set; }
        public string? BookingNumber { get; set; }
        public string? Email { get; set; }
    }
}
