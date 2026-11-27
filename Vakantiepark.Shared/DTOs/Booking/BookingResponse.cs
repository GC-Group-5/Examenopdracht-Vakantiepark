namespace Vakantiepark.Shared.DTOs.Booking
{
    public class BookingResponse
    {
        public string BookingNumber { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public DateOnly ArrivalDate { get; set; }
        public DateOnly DepartureDate { get; set; }
        public TimeOnly ArrivalTime { get; set; }
        public TimeOnly DepartureTime { get; set; }
        public int NumberOfGuests { get; set; }
        public int NumberOfChildrenUnder12 { get; set; }
        public int NumberOfPets { get; set; }
        public bool WheelchairAccessible { get; set; }
        public decimal AccommodationPrice { get; set; }
        public decimal CleaningFee { get; set; }
        public decimal PetCleaningFee { get; set; }
        public decimal TouristTax { get; set; }
        public decimal SecurityDeposit { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal Deposit { get; set; }

        // US-46: the deposit must be transferred by this date.
        public DateOnly DepositDueDate { get; set; }
    }
}
