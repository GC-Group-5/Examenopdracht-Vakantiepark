using System.ComponentModel.DataAnnotations;

namespace Vakantiepark.Shared.DTOs.Reservations
{
    public class StaffCancelBookingRequest
    {
        [Required(ErrorMessage = "Vul de reden van annulering in.")]
        [StringLength(500, ErrorMessage = "De reden mag maximaal 500 tekens zijn.")]
        public string Reason { get; set; } = string.Empty;
    }
}
