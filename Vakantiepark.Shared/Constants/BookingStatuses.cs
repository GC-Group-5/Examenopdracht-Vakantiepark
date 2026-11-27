namespace Vakantiepark.Shared.Constants
{
    // Shown on "Mijn boeking"; the frontend picks the badge colour by these exact texts.
    public static class BookingStatuses
    {
        public const string InAfwachting = "In afwachting van aanbetaling";
        public const string Bevestigd = "Bevestigd";
        public const string Ingecheckt = "Ingecheckt";
        public const string Uitgecheckt = "Uitgecheckt";
        public const string Geannuleerd = "Geannuleerd";

        // US-46: shown instead of InAfwachting once the due date has passed. Not stored: it follows from the date.
        public const string AanbetalingTeLaat = "Aanbetaling te laat";
    }
}
