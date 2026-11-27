using Vakantiepark.Domain.Enums;
using Vakantiepark.Domain.Interfaces;

namespace Vakantiepark.Domain.Entities
{
    public class Booking : IEntity
    {
        public Guid Id { get; set; }
        public string BookingNumber { get; set; } = string.Empty;

        // Secret, unguessable key in the personal link to "Mijn boeking" in the confirmation email.
        public string AccessToken { get; set; } = string.Empty;

        // Contact details of the main booker; everyone who stays is in Guests.
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;

        public DateOnly ArrivalDate { get; set; }
        public DateOnly DepartureDate { get; set; }

        // Copied from the park settings at booking time, so a later change does not alter this booking.
        public TimeOnly ArrivalTime { get; set; }
        public TimeOnly DepartureTime { get; set; }
        // Empty for bookings made before US-18 stored them; those follow the current park settings.
        public int? FreeCancellationDaysBeforeArrival { get; set; }
        public int? NoRefundDaysBeforeArrival { get; set; }

        public int NumberOfGuests { get; set; }
        public int NumberOfChildrenUnder12 { get; set; }
        public int NumberOfPets { get; set; }
        public bool WheelchairAccessible { get; set; }

        // The price is fixed when booking; a later rate change does not alter it (US-07).
        public decimal AccommodationPrice { get; set; }
        public decimal CleaningFee { get; set; }
        public decimal PetCleaningFee { get; set; }
        public decimal TouristTax { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal Deposit { get; set; }
        public decimal? CancellationFee { get; set; }
        public decimal SecurityDeposit { get; set; }

        // US-46: a new booking waits for its deposit; staff move it on (paid, checked in, checked out).
        public BookingState Status { get; set; } = BookingState.AwaitingDeposit;

        // Arrival minus the free-cancellation term, or the booking date when that is already past.
        public DateOnly DepositDueDate { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        // Navigation Keys
        public Guid AccommodationUnitId { get; set; }

        // Navigation Properties
        public Accommodation AccommodationUnit { get; set; } = null!;
        public List<BookingGuest> Guests { get; set; } = [];
        public List<BookingChange> Changes { get; set; } = [];
    }
}
