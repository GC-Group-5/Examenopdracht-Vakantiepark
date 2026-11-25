namespace Vakantiepark.Shared.DTOs.Occupancy
{
    // The occupancy overview for one date (US-16): the number of units per status and one page of units.
    public class OccupancyResponse
    {
        public DateOnly Date { get; set; }
        public int AvailableCount { get; set; }
        public int OccupiedCount { get; set; }
        public int OutOfServiceCount { get; set; }
        public PagedResponse<OccupancyUnitResponse> Units { get; set; } = new();
    }
}
