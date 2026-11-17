namespace Vakantiepark.Shared.DTOs.Maintenance
{
    public class MaintenancePeriodResponse
    {
        public Guid Id { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }

        // Maintenance, TechnicalProblem or Other.
        public string Reason { get; set; } = string.Empty;
        public string? Note { get; set; }

        // Bookings that overlap this period and need staff attention (US-17, criterion 3).
        public List<BookingConflictResponse> ConflictingBookings { get; set; } = [];
    }

    public class MaintenanceUnitOverviewResponse
    {
        public string UnitCode { get; set; } = string.Empty;
        public string UnitName { get; set; } = string.Empty;
        public List<MaintenancePeriodResponse> Periods { get; set; } = [];
    }

    public class BookingConflictResponse
    {
        public Guid BookingId { get; set; }
        public string BookingNumber { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DateOnly ArrivalDate { get; set; }
        public DateOnly DepartureDate { get; set; }
        public int NumberOfGuests { get; set; }
    }

    // Result of registering or changing a period.
    public class MaintenancePeriodSaveResult
    {
        public bool Saved { get; set; }

        // True when bookings overlap and the request did not confirm them yet;
        // the period is not saved until the staff confirms explicitly.
        public bool RequiresConfirmation { get; set; }

        public string? Message { get; set; }
        public MaintenancePeriodResponse? Period { get; set; }
        public List<BookingConflictResponse> ConflictingBookings { get; set; } = [];
    }
}
