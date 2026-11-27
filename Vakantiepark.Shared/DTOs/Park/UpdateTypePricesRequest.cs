using System.ComponentModel.DataAnnotations;

namespace Vakantiepark.Shared.DTOs.Park
{
    public class UpdateTypePricesRequest
    {
        [Required(ErrorMessage = "Vul de standaardprijs per nacht in.")]
        // The limits are written with a dot; the browser runs in nl-NL, where "0.01" is not a number (US-53 bug).
        [Range(typeof(decimal), "0.01", "100000", ParseLimitsInInvariantCulture = true, ErrorMessage = "De standaardprijs moet hoger dan 0 zijn.")]
        public decimal? DefaultPricePerNight { get; set; }

        [Required(ErrorMessage = "Vul de eindschoonmaakkosten in.")]
        [Range(typeof(decimal), "0", "100000", ErrorMessage = "De eindschoonmaakkosten mogen niet negatief zijn.")]
        public decimal? CleaningFee { get; set; }

        public List<SeasonPriceRequest> SeasonPrices { get; set; } = [];
    }

    // Empty means the season has no own price yet, so its nights use the default price.
    public class SeasonPriceRequest
    {
        public Guid SeasonId { get; set; }

        [Range(typeof(decimal), "0.01", "100000", ParseLimitsInInvariantCulture = true, ErrorMessage = "Een seizoensprijs moet hoger dan 0 zijn.")]
        public decimal? PricePerNight { get; set; }
    }

    public class TypePricesResponse : UpdateTypePricesRequest
    {
        public string Slug { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
