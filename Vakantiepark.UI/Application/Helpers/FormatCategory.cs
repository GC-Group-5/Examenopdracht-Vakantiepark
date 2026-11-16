namespace Vakantiepark.UI.Application.Helpers
{
    public static class FormatCategory
    {
        public static string Format(string? category)
            => category switch
            {
                "CampingPitch" => "Kampeerplaats",
                "HolidayHome" => "Vakantiewoning",
                _ => category ?? "Verblijf",
            };
    }
}
