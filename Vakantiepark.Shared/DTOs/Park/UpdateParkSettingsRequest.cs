using System.ComponentModel.DataAnnotations;

namespace Vakantiepark.Shared.DTOs.Park
{
    // Nullable so an empty field gets a Dutch "required" message instead of silently becoming 0.
    public class UpdateParkSettingsRequest
    {
        [Required(ErrorMessage = "Vul de aankomsttijd in.")]
        public TimeOnly? ArrivalTime { get; set; }

        [Required(ErrorMessage = "Vul de vertrektijd in.")]
        public TimeOnly? DepartureTime { get; set; }

        [Required(ErrorMessage = "Vul het minimum aantal nachten in het hoogseizoen in.")]
        [Range(1, 60, ErrorMessage = "Het minimum aantal nachten moet tussen 1 en 60 liggen.")]
        public int? HighSeasonMinimumNights { get; set; }

        [Required(ErrorMessage = "Vul het aanbetalingspercentage in.")]
        [Range(typeof(decimal), "0", "100", ErrorMessage = "Het aanbetalingspercentage moet tussen 0 en 100 liggen.")]
        public decimal? DepositPercentage { get; set; }

        [Required(ErrorMessage = "Vul in tot hoeveel dagen voor aankomst gratis annuleren kan.")]
        [Range(0, 365, ErrorMessage = "Vul een aantal dagen tussen 0 en 365 in.")]
        public int? FreeCancellationDaysBeforeArrival { get; set; }

        [Required(ErrorMessage = "Vul in hoeveel dagen voor aankomst de aanbetaling binnen moet zijn.")]
        [Range(0, 365, ErrorMessage = "Vul een aantal dagen tussen 0 en 365 in.")]
        public int? DepositDueDaysBeforeArrival { get; set; }

        [Required(ErrorMessage = "Vul in vanaf hoeveel dagen voor aankomst er niets wordt terugbetaald.")]
        [Range(0, 365, ErrorMessage = "Vul een aantal dagen tussen 0 en 365 in.")]
        public int? NoRefundDaysBeforeArrival { get; set; }

        [Required(ErrorMessage = "Vul de schoonmaakkosten voor huisdieren in.")]
        [Range(typeof(decimal), "0", "10000", ErrorMessage = "De schoonmaakkosten voor huisdieren mogen niet negatief zijn.")]
        public decimal? PetCleaningFee { get; set; }

        [Required(ErrorMessage = "Vul het bedrag van de borg in.")]
        [Range(typeof(decimal), "0", "10000", ErrorMessage = "De borg mag niet negatief zijn.")]
        public decimal? SecurityDeposit { get; set; }

        // US-46: where the guest transfers the deposit. An IBAN is at most 34 characters.
        [Required(ErrorMessage = "Vul het rekeningnummer voor de aanbetaling in.")]
        [StringLength(34, ErrorMessage = "Het rekeningnummer mag maximaal 34 tekens zijn.")]
        public string BankAccountNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vul de tenaamstelling van de rekening in.")]
        [StringLength(100, ErrorMessage = "De tenaamstelling mag maximaal 100 tekens zijn.")]
        public string BankAccountName { get; set; } = string.Empty;
    }
}
