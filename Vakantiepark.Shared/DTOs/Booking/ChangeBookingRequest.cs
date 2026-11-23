namespace Vakantiepark.Shared.DTOs.Booking
{
    public class ChangeBookingRequest : BookingAccessRequest
    {
        public DateOnly ArrivalDate { get; set; }
        public DateOnly DepartureDate { get; set; }

        public int NumberOfGuests { get; set; }

        // Fellow travellers are only counted, as when booking (US-38).
        public int NumberOfChildrenUnder12 { get; set; }
    }
}
