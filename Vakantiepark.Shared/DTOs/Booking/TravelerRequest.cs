using System.ComponentModel.DataAnnotations;

namespace Vakantiepark.Shared.DTOs.Booking
{
    public class TravelerRequest
    {
        [Required(ErrorMessage = "Vul de naam van je reisgenoot in.")]
        [MaxLength(100, ErrorMessage = "Een naam mag maximaal 100 tekens zijn.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vul de geboortedatum van je reisgenoot in.")]
        public DateOnly? BirthDate { get; set; }
    }
}
