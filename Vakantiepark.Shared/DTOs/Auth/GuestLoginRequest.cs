using System.ComponentModel.DataAnnotations;

namespace Vakantiepark.Shared.DTOs.Auth
{
    public class GuestLoginRequest
    {
        [Required(ErrorMessage = "Vul je boekingsnummer in.")]
        [MaxLength(20, ErrorMessage = "Het boekingsnummer mag maximaal {1} tekens bevatten.")]
        public string BookingNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vul je e-mailadres in.")]
        [EmailAddress(ErrorMessage = "Vul een geldig e-mailadres in.")]
        public string Email { get; set; } = string.Empty;
    }
}
