namespace Vakantiepark.Shared.DTOs.Booking
{
    public class ChangeBookingRequest : BookingAccessRequest
    {
        public DateOnly ArrivalDate { get; set; }
        public DateOnly DepartureDate { get; set; }

        public int NumberOfGuests { get; set; }

        public int NumberOfChildrenUnder12 { get; set; }

        // Optional, as when booking.
        public List<TravellerRequest> CoTravellers { get; set; } = [];
    }
}
