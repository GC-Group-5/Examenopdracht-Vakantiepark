using System.ComponentModel.DataAnnotations;

namespace Vakantiepark.UI.Application.DTOs
{
    public class ContactMessage
    {
        [Required(ErrorMessage = "Vul je naam in.")]
        [MaxLength(100, ErrorMessage = "Je naam mag maximaal {1} tekens bevatten.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vul je e-mailadres in.")]
        [EmailAddress(ErrorMessage = "Vul een geldig e-mailadres in.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Kies een onderwerp.")]
        public string Subject { get; set; } = "Algemene vraag";

        [Required(ErrorMessage = "Schrijf je bericht.")]
        [MaxLength(2000, ErrorMessage = "Je bericht mag maximaal {1} tekens bevatten.")]
        public string Message { get; set; } = string.Empty;
    }
}
