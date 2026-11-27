namespace Vakantiepark.Shared.Constants
{
    // US-51: why a unit can or cannot take a changed booking; the edit screen greys out all but Vrij.
    public static class UnitAvailability
    {
        public const string Vrij = "Vrij";
        public const string Geboekt = "Geboekt";
        public const string Onderhoud = "Onderhoud";
        public const string PastNiet = "Past niet";
    }
}
