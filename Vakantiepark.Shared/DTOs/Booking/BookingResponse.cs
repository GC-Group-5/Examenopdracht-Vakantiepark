namespace Vakantiepark.Shared.DTOs.Booking
{
    public class BookingResponse
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public DateOnly ArrivalDate { get; set; }
        public DateOnly DepartureDate { get; set; }
        public TimeOnly ArrivalTime { get; set; }
        public TimeOnly DepartureTime { get; set; }
        public int NumberOfGuests { get; set; }
        public decimal AccommodationPrice { get; set; }
        public decimal CleaningFee { get; set; }
        public decimal TouristTax { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
