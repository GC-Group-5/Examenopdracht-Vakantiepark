namespace Vakantiepark.Shared.DTOs.Cleaning
{
    // US-32: the units that become empty on one day. Deliberately without names or contact details of guests.
    public class CleaningDayResponse
    {
        public DateOnly Date { get; set; }
        public List<CleaningItemResponse> Units { get; set; } = [];
    }

    public class CleaningItemResponse
    {
        // Identifies the departure, so the cleaner can mark it as busy or done.
        public Guid BookingId { get; set; }

        public string UnitCode { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty;
        public string Zone { get; set; } = string.Empty;

        // New guests come the same day, so this unit has to be cleaned first.
        public bool GuestsArriveToday { get; set; }

        public string Status { get; set; } = string.Empty;

        // Only once someone has started: who is busy, or who cleaned it and when.
        public string? CleanedBy { get; set; }
        public DateTime? FinishedAt { get; set; }
    }
}
