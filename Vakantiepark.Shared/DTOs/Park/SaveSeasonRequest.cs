using System.ComponentModel.DataAnnotations;

namespace Vakantiepark.Shared.DTOs.Park
{
    public class SaveSeasonRequest
    {
        [Required(ErrorMessage = "Vul de naam van het seizoen in.")]
        [StringLength(40, ErrorMessage = "De naam mag maximaal 40 tekens zijn.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vul de begindatum in.")]
        public DateOnly? StartDate { get; set; }

        [Required(ErrorMessage = "Vul de einddatum in.")]
        public DateOnly? EndDate { get; set; }

        public bool IsHighSeason { get; set; }
    }

    public class SeasonResponse : SaveSeasonRequest
    {
        public Guid Id { get; set; }
    }
}
