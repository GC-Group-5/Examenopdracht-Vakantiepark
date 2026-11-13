using Vakantiepark.Domain.Interfaces;

namespace Vakantiepark.Domain.Entities
{
    public class Booking : IEntity
    {
        public Guid Id { get; set; }
        public string BookingNumber { get; set; } = string.Empty;

        // Contact details of the main booker; everyone who stays is in Guests.
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;

        public DateOnly ArrivalDate { get; set; }
        public DateOnly DepartureDate { get; set; }

        // Copied from the park settings at booking time, so a later change does not alter this booking.
        public TimeOnly ArrivalTime { get; set; }
        public TimeOnly DepartureTime { get; set; }

        public int NumberOfGuests { get; set; }

        // The price is fixed when booking; a later rate change does not alter it (US-07).
        public decimal AccommodationPrice { get; set; }
        public decimal CleaningFee { get; set; }
        public decimal TouristTax { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal Deposit { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        // Navigation Keys
        public Guid AccommodationUnitId { get; set; }

        // Navigation Properties
        public Accommodation AccommodationUnit { get; set; } = null!;
        public List<BookingGuest> Guests { get; set; } = [];
    }
}
