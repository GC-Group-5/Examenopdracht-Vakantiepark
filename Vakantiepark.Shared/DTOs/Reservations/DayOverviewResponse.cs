namespace Vakantiepark.Shared.DTOs.Reservations
{
    // Who arrives and who departs on one day (US-15), each list sorted by zone and unit.
    public class DayOverviewResponse
    {
        public DateOnly Date { get; set; }
        public List<DayOverviewItemResponse> Arrivals { get; set; } = [];
        public List<DayOverviewItemResponse> Departures { get; set; } = [];
    }
}
