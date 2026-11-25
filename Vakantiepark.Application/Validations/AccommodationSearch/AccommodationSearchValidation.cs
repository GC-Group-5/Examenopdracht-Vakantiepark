using Vakantiepark.Domain.Entities;
using Vakantiepark.Domain.Enums;

namespace Vakantiepark.Application.Validations.AccommodationSearch
{
    public static class AccommodationSearchValidation
    {
        public static void ValidateSearchPeriod(this DateOnly arrivalDate, DateOnly departureDate, int numberOfGuests, int numberOfPets, DateOnly today)
        {
            if (numberOfGuests < 1)
                throw new ArgumentException("Kies minimaal één persoon.");

            if (numberOfPets < 0)
                throw new ArgumentException("Het aantal huisdieren kan niet negatief zijn.");

            if (arrivalDate < today)
                throw new ArgumentException("De aankomstdatum ligt in het verleden. Kies een latere aankomstdatum.");

            if (departureDate <= arrivalDate)
                throw new ArgumentException("De vertrekdatum moet na de aankomstdatum liggen.");
        }

        // Optional filter on holiday homes or camping pitches; empty means both.
        public static AccommodationCategory? ParseCategory(string? category)
        {
            if (string.IsNullOrWhiteSpace(category))
                return null;

            if (Enum.TryParse(category, true, out AccommodationCategory parsed) && Enum.IsDefined(parsed))
                return parsed;

            throw new ArgumentException("Kies vakantiewoning of kampeerplaats als soort verblijf.");
        }

        // US-06: when the period falls (partly) in a high season, the whole stay must
        // meet the minimum number of nights; the visitor then sees no results but a
        // message naming the minimum, not a booking rejection.
        public static string? GetHighSeasonSearchMessage(this List<Season> seasons, ParkSettings? settings, DateOnly arrivalDate, DateOnly departureDate)
        {
            if (settings is null)
                return null;

            int nights = departureDate.DayNumber - arrivalDate.DayNumber;
            Season? highSeason = seasons
                .Where(season => season.IsHighSeason)
                .FirstOrDefault(season => season.StartDate < departureDate && arrivalDate <= season.EndDate);

            if (highSeason is null || nights >= settings.HighSeasonMinimumNights)
                return null;

            return $"In het hoogseizoen ({highSeason.Name}) moet een boeking minimaal {settings.HighSeasonMinimumNights} nachten zijn. Pas je aankomst- of vertrekdatum aan.";
        }
    }
}
