using System.ComponentModel.DataAnnotations;

namespace Vakantiepark.Shared.DTOs.Park
{
    public class AddTouristTaxRateRequest
    {
        [Required(ErrorMessage = "Vul het bedrag per persoon per nacht in.")]
        [Range(typeof(decimal), "0", "1000", ErrorMessage = "De toeristenbelasting mag niet negatief zijn.")]
        public decimal? AmountPerPersonPerNight { get; set; }

        [Required(ErrorMessage = "Vul de ingangsdatum in.")]
        public DateOnly? ValidFrom { get; set; }
    }
}
