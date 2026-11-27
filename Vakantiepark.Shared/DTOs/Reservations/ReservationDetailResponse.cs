using Vakantiepark.Shared.DTOs.Booking;

namespace Vakantiepark.Shared.DTOs.Reservations
{
    // Everything the guest sees on "Mijn boeking", plus what only staff needs (US-13).
    public class ReservationDetailResponse : MyBookingResponse
    {
        public Guid Id { get; set; }
        public string UnitCode { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        // US-32: empty until the guests leave; afterwards how far the cleaning of the unit is.
        public string? CleaningStatus { get; set; }
        public string? CleanedBy { get; set; }
        public DateTime? CleanedAt { get; set; }

        // Newest first (US-14).
        public List<BookingChangeResponse> Changes { get; set; } = [];
    }
}
