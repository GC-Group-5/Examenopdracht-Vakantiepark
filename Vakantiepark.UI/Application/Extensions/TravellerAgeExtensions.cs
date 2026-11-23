using System.Globalization;
using Vakantiepark.Shared.Constants;
using Vakantiepark.UI.Application.Helpers;

namespace Vakantiepark.UI.Application.Extensions
{
    public static class TravellerAgeExtensions
    {
        // Same rule as the server: someone born on 29 February turns a year older on 1 March.
        public static int AgeOn(this DateOnly birthDate, DateOnly date)
        {
            int age = date.Year - birthDate.Year;
            if (birthDate > date.AddYears(-age))
                age--;
            return age;
        }

        // The age group on the arrival date, so reception sees at once who pays tourist tax.
        public static string AgeGroupLabel(this DateOnly birthDate, DateOnly arrivalDate)
        {
            int age = birthDate.AgeOn(arrivalDate);
            if (age >= BookingRules.MinimumMainBookerAge)
                return "volwassene";
            if (age >= BookingRules.TouristTaxMinimumAge)
                return $"{BookingRules.TouristTaxMinimumAge} t/m {BookingRules.MinimumMainBookerAge - 1} jaar";
            return $"jonger dan {BookingRules.TouristTaxMinimumAge}, geen toeristenbelasting";
        }

        // e.g. "Els Jansen (12-09-1987) · volwassene"; without a birth date reception completes it.
        public static string TravellerLabel(string name, DateOnly? birthDate, DateOnly arrivalDate, bool isMainBooker = false)
        {
            string details = birthDate is DateOnly date
                ? $" ({date.ToString(DateFormats.Date, CultureInfo.InvariantCulture)}) · {date.AgeGroupLabel(arrivalDate)}"
                : " · geboortedatum nog onbekend";
            return name.Trim() + details + (isMainBooker ? " · hoofdboeker" : "");
        }

        // The persons of whom not even a name is known yet.
        public static string? MissingTravellersLabel(int numberOfGuests, int numberKnown)
        {
            int missing = numberOfGuests - numberKnown;
            return missing <= 0 ? null
                : $"Nog {missing} {(missing == 1 ? "persoon" : "personen")} zonder gegevens; de receptie vult dit aan bij het inchecken.";
        }
    }
}
