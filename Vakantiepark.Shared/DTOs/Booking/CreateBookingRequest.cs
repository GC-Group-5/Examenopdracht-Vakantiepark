using System.ComponentModel.DataAnnotations;

namespace Vakantiepark.Shared.DTOs.Booking
{
    // What the guest fills in. Arrival and departure times are not part of it: the server
    // takes them from the park settings, so a browser can never choose its own times.
    public class CreateBookingRequest
    {
        public string AccommodationTypeSlug { get; set; } = string.Empty;
        public DateOnly ArrivalDate { get; set; }
        public DateOnly DepartureDate { get; set; }

        [Range(1, 20, ErrorMessage = "Kies tussen 1 en 20 personen.")]
        public int NumberOfGuests { get; set; }

        // Optional zone name from the search, e.g. 'Waterkant'.
        public string? PreferredZone { get; set; }

        [Required(ErrorMessage = "Vul je naam in.")]
        [StringLength(100, ErrorMessage = "Je naam mag maximaal 100 tekens zijn.")]
        public string Name { get; set; } = string.Empty;

        // Stricter than [EmailAddress], which also accepts addresses without a domain extension such as "jan@gmail".
        [Required(ErrorMessage = "Vul je e-mailadres in.")]
        [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]{2,}$", ErrorMessage = "Vul een geldig e-mailadres in, bijvoorbeeld naam@voorbeeld.nl.")]
        public string Email { get; set; } = string.Empty;

        // 9 to 15 digits, optionally starting with + or 00, with spaces or dashes allowed between digits.
        [Required(ErrorMessage = "Vul je telefoonnummer in.")]
        [RegularExpression(@"^(\+|00)?[0-9]([\s-]?[0-9]){8,14}$", ErrorMessage = "Vul een geldig telefoonnummer in, bijvoorbeeld 06 12345678 of +31 6 12345678.")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vul je geboortedatum in.")]
        public DateOnly? BirthDate { get; set; }

        // Everyone except the main booker: NumberOfGuests - 1 persons.
        public List<TravellerRequest> CoTravellers { get; set; } = [];
    }

    public class TravellerRequest
    {
        [Required(ErrorMessage = "Vul de naam van iedere reisgenoot in.")]
        [StringLength(100, ErrorMessage = "Een naam mag maximaal 100 tekens zijn.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vul de geboortedatum van iedere reisgenoot in.")]
        public DateOnly? BirthDate { get; set; }
    }
}
