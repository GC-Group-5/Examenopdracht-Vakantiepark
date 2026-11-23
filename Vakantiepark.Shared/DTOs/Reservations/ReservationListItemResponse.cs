namespace Vakantiepark.Shared.DTOs.Reservations
{
    // One row in the reservations overview (US-13).
    public class ReservationListItemResponse
    {
        public Guid Id { get; set; }
        public string BookingNumber { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string AccommodationTypeName { get; set; } = string.Empty;
        public string UnitCode { get; set; } = string.Empty;
        public DateOnly ArrivalDate { get; set; }
        public DateOnly DepartureDate { get; set; }
        public int NumberOfGuests { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
