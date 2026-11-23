namespace Vakantiepark.Shared.Constants
{
    // Rules the frontend checks right away and the API checks again.
    public static class BookingRules
    {
        public const int MinimumMainBookerAge = 18;

        // From this age a person pays tourist tax (US-38).
        public const int TouristTaxMinimumAge = 12;
    }
}