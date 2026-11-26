namespace Vakantiepark.UI.Application.Helpers
{
    // Wording for a stay, the same on the search, booking and reservation pages.
    public static class StayLabels
    {
        public static int NumberOfNights(DateOnly arrivalDate, DateOnly departureDate)
            => departureDate.DayNumber - arrivalDate.DayNumber;

        // e.g. "1 nacht", "7 nachten"
        public static string Nights(int numberOfNights)
            => numberOfNights == 1 ? "1 nacht" : $"{numberOfNights} nachten";

        // e.g. "1 huisdier", "2 huisdieren"
        public static string Pets(int numberOfPets)
            => numberOfPets == 1 ? "1 huisdier" : $"{numberOfPets} huisdieren";
    }
}
