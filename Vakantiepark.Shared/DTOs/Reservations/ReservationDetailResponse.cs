using Vakantiepark.Shared.DTOs.Booking;

namespace Vakantiepark.Shared.DTOs.Reservations
{
    // Everything the guest sees on "Mijn boeking", plus what only staff needs (US-13).
    public class ReservationDetailResponse : MyBookingResponse
    {
        public Guid Id { get; set; }
        public string UnitCode { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
