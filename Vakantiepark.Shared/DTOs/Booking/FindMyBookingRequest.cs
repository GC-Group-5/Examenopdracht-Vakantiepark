using System.ComponentModel.DataAnnotations;

namespace Vakantiepark.Shared.DTOs.Booking
{
    // Logging in on "Mijn boeking" without the link from the email.
    public class FindMyBookingRequest
    {
        [Required(ErrorMessage = "Vul je boekingsnummer in.")]
        public string BookingNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vul je e-mailadres in.")]
        public string Email { get; set; } = string.Empty;
    }
}
