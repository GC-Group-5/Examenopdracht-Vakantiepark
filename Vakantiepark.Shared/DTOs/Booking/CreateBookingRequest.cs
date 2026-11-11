using System.ComponentModel.DataAnnotations;

namespace Vakantiepark.Shared.DTOs.Booking
{
    public class CreateBookingRequest
    {
        public string AccommodationTypeSlug { get; set; } = string.Empty;
        public DateOnly ArrivalDate { get; set; }
        public DateOnly DepartureDate { get; set; }
        public TimeOnly ArrivalTime { get; set; }
        public TimeOnly DepartureTime { get; set; }
        public int NumberOfGuests { get; set; }

        [Required(ErrorMessage = "Vul je naam in.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vul je e-mailadres in.")]
        [EmailAddress(ErrorMessage = "Vul een geldig e-mailadres in.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vul je telefoonnummer in.")]
        public string PhoneNumber { get; set; } = string.Empty;
    }
}
