namespace Vakantiepark.Shared.DTOs.Booking
{
    public class ChangeBookingRequest : BookingAccessRequest
    {
        public DateOnly ArrivalDate { get; set; }
        public DateOnly DepartureDate { get; set; }

        public int NumberOfGuests { get; set; }

        // Optional, as when booking; the birth dates decide who is under 12.
        public List<TravellerRequest> CoTravellers { get; set; } = [];
    }
}
