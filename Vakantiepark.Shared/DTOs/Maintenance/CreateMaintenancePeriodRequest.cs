namespace Vakantiepark.Shared.DTOs.Maintenance
{
    public class CreateMaintenancePeriodRequest
    {
        public string AccommodationUnitCode { get; set; } = string.Empty;
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }

        // Maintenance, TechnicalProblem or Other.
        public string Reason { get; set; } = string.Empty;
        public string? Note { get; set; }

        // Staff must explicitly confirm when existing bookings overlap the period.
        public bool ConfirmExistingBookings { get; set; }
    }

    public class UpdateMaintenancePeriodRequest
    {
        // New dates; the period can only be shortened or ended, never widened.
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
    }
}
