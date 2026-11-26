namespace Vakantiepark.UI.Application.Helpers
{
    // The client wants every date as dd-mm-jjjj (US-38); use these instead of a format per page.
    public static class DateFormats
    {
        public const string Date = "dd-MM-yyyy";
        public const string Time = "HH:mm";
        public const string DateAndTime = "dd-MM-yyyy HH:mm";
        public const string DayAndDate = "dddd dd-MM-yyyy";

        // Dates in page addresses and API calls; never shown to the user.
        public const string Query = "yyyy-MM-dd";
    }
}
