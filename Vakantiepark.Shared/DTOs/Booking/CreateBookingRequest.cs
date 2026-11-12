using System.ComponentModel.DataAnnotations;

namespace Vakantiepark.Shared.DTOs.Booking
{
    public class CreateBookingRequest
    {
        public string AccommodationTypeSlug { get; set; } = string.Empty;
        public DateOnly ArrivalDate { get; set; }
        public DateOnly DepartureDate { get; set; }
        public int NumberOfGuests { get; set; }

        // Optional; empty means no preference and any free unit of the type is assigned.
        public string? PreferredZone { get; set; }

        [Required(ErrorMessage = "Vul je naam in.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vul je geboortedatum in.")]
        public DateOnly? BirthDate { get; set; }

        // Stricter than [EmailAddress], which also accepts addresses without a domain extension such as "jan@gmail".
        [Required(ErrorMessage = "Vul je e-mailadres in.")]
        [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]{2,}$", ErrorMessage = "Vul een geldig e-mailadres in, bijvoorbeeld naam@voorbeeld.nl.")]
        public string Email { get; set; } = string.Empty;

        // 9 to 15 digits, optionally starting with + or 00, with spaces or dashes allowed between digits.
        [Required(ErrorMessage = "Vul je telefoonnummer in.")]
        [RegularExpression(@"^(\+|00)?[0-9]([\s-]?[0-9]){8,14}$", ErrorMessage = "Vul een geldig telefoonnummer in, bijvoorbeeld 06 12345678 of +31 6 12345678.")]
        public string PhoneNumber { get; set; } = string.Empty;

        // Everyone in the party except the main booker, so NumberOfGuests - 1 travelers.
        public List<TravelerRequest> Travelers { get; set; } = [];
    }
}
