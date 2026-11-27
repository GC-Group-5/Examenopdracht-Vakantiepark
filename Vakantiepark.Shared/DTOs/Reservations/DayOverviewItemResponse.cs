namespace Vakantiepark.Shared.DTOs.Reservations
{
    // One arriving or departing booking in the day overview (US-15).
    public class DayOverviewItemResponse
    {
        public Guid Id { get; set; }
        public string BookingNumber { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string AccommodationTypeName { get; set; } = string.Empty;
        public string UnitCode { get; set; } = string.Empty;
        public string Zone { get; set; } = string.Empty;
        public int NumberOfGuests { get; set; }
        public int NumberOfPets { get; set; }

        // US-32: only filled for departures, so reception can see which units are ready for new guests.
        public string? CleaningStatus { get; set; }
        public string? CleanedBy { get; set; }
    }
}
