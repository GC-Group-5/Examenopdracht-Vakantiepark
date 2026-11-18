namespace Vakantiepark.Shared.DTOs.Booking
{
	public class ChangeBookingRequest : BookingAccessRequest
	{
		public DateOnly ArrivalDate { get; set; }
		public DateOnly DepartureDate { get; set; }

		public int NumberOfGuests { get; set; }

		public List<TravellerRequest> CoTravellers { get; set; } = [];
	}
}
