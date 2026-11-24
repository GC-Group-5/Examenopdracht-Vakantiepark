namespace Vakantiepark.Shared.DTOs.Reservations
{
    // Query of the reservations overview (US-13). Status is one of BookingStatuses or empty for all.
    public class ReservationSearchRequest : PagedRequest
    {
        public string? Search { get; set; }
        public DateOnly? From { get; set; }
        public DateOnly? To { get; set; }
        public string? Status { get; set; }
        public string? AccommodationTypeSlug { get; set; }
    }
}
