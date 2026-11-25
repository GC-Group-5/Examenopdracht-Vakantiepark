using Vakantiepark.Domain.Entities;

namespace Vakantiepark.Application.Validations.Seasons
{
    public static class SeasonValidation
    {
        public static void ValidateNoOverlap(this Season season, IEnumerable<Season> existingSeasons)
        {
            if (season.StartDate > season.EndDate)
                throw new ArgumentException("De begindatum van een seizoen moet vóór de einddatum liggen.");

            if (existingSeasons.Any(existing => existing.Id != season.Id
                && season.StartDate <= existing.EndDate
                && season.EndDate >= existing.StartDate))
                throw new ArgumentException("Dit seizoen overlapt een bestaand seizoen.");
        }

        public static void ValidateUniqueName(this Season season, IEnumerable<Season> existingSeasons)
        {
            if (existingSeasons.Any(existing => existing.Id != season.Id && string.Equals(existing.Name, season.Name, StringComparison.OrdinalIgnoreCase)))
                throw new ArgumentException("Er bestaat al een seizoen met deze naam.");
        }
    }
}
