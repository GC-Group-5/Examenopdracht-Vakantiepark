using System.ComponentModel.DataAnnotations;
using Vakantiepark.Shared.DTOs.Booking;

namespace Vakantiepark.Shared.DTOs.Reservations
{
    public class StaffChangeBookingRequest : ChangeBookingRequest
    {
        [Required(ErrorMessage = "Vul de naam van de hoofdboeker in.")]
        [StringLength(100, ErrorMessage = "De naam mag maximaal 100 tekens zijn.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vul het e-mailadres in.")]
        [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]{2,}$", ErrorMessage = "Vul een geldig e-mailadres in, bijvoorbeeld naam@voorbeeld.nl.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vul het telefoonnummer in.")]
        [RegularExpression(@"^(\+|00)?[0-9]([\s-]?[0-9]){8,14}$", ErrorMessage = "Vul een geldig telefoonnummer in, bijvoorbeeld 06 12345678.")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vul de geboortedatum van de hoofdboeker in.")]
        public DateOnly? BirthDate { get; set; }

        // Empty: keep the current unit when it is still free, as for a guest.
        public string? AccommodationUnitCode { get; set; }
    }
}