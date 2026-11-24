namespace Vakantiepark.Shared.DTOs.Occupancy
{
    // Query of the occupancy overview (US-16). An empty date means today; empty filters mean all.
    // Category is HolidayHome or CampingPitch.
    public class OccupancyRequest : PagedRequest
    {
        public DateOnly? Date { get; set; }
        public string? AccommodationTypeSlug { get; set; }
        public Guid? ZoneId { get; set; }
        public string? Category { get; set; }
    }
}
