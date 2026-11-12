using System.ComponentModel.DataAnnotations;

namespace Vakantiepark.Shared.DTOs.Auth
{
    public class LoginRequest
    {
        [Required(ErrorMessage = "Vul je e-mailadres in.")]
        [EmailAddress(ErrorMessage = "Vul een geldig e-mailadres in.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vul je wachtwoord in.")]
        public string Password { get; set; } = string.Empty;
    }
}
