namespace Vakantiepark.Shared.DTOs.Reservations
{
    // One line of the change log on the reservation detail page (US-14).
    public class BookingChangeResponse
    {
        public DateTime ChangedAt { get; set; }
        public string StaffName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Reason { get; set; }
    }
}
