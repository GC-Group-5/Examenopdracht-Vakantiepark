namespace Vakantiepark.Shared.DTOs.Occupancy
{
    // One unit in the occupancy overview, with the booking or the maintenance reason behind its status.
    public class OccupancyUnitResponse
    {
        public string Code { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty;
        public string ZoneName { get; set; } = string.Empty;
        // One of OccupancyStatuses.
        public string Status { get; set; } = string.Empty;

        public Guid? BookingId { get; set; }
        public string? BookingNumber { get; set; }
        public string? GuestName { get; set; }

        // Maintenance, TechnicalProblem or Other.
        public string? MaintenanceReason { get; set; }
        public string? MaintenanceNote { get; set; }
    }
}
