namespace Vakantiepark.UI.Application.Helpers
{
    // One wording for the cards and the detail page, e.g. "gemiddeld € 87 per nacht".
    public static class FormatPrice
    {
        public static string AveragePerNight(decimal price)
            => $"gemiddeld {price:C0} per nacht";
    }
}
