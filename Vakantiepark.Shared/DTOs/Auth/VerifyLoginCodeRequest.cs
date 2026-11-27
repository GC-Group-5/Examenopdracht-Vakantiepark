using System.ComponentModel.DataAnnotations;

namespace Vakantiepark.Shared.DTOs.Auth
{
    public class VerifyLoginCodeRequest
    {
        [Required(ErrorMessage = "Vul je e-mailadres in.")]
        [EmailAddress(ErrorMessage = "Vul een geldig e-mailadres in.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vul de code uit de e-mail in.")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "De code bestaat uit 6 cijfers.")]
        public string Code { get; set; } = string.Empty;

        public bool RememberDevice { get; set; }
    }
}
