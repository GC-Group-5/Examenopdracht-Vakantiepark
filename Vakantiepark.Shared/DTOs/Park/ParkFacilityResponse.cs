namespace Vakantiepark.Shared.DTOs.Park
{
    // One item of park information in the PWA (US-20).
    public class ParkFacilityResponse
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        // FoodAndDrink, Supermarket, Activity or Facility.
        public string Category { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string PhotoUrl { get; set; } = string.Empty;

        // Park time; empty for activities.
        public TimeOnly? OpensAt { get; set; }
        public TimeOnly? ClosesAt { get; set; }

        // UTC; only for activities.
        public DateTime? StartsAt { get; set; }
        public DateTime? EndsAt { get; set; }

        // Open right now, or for an activity: taking place right now.
        public bool IsOpen { get; set; }
    }
}
