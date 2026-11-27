namespace Vakantiepark.Application.Extensions
{
    public static class TimeProviderExtensions
    {
        // Opening hours are Dutch clock times, while the server runs in UTC.
        private const string ParkTimeZoneId = "Europe/Amsterdam";

        // Stays are whole days without a time zone, so every rule compares against the same UTC date.
        public static DateOnly GetToday(this TimeProvider timeProvider)
            => DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        // DateTime gives the clock on the park, UtcDateTime the same moment in UTC.
        public static DateTimeOffset GetParkNow(this TimeProvider timeProvider)
            => TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), TimeZoneInfo.FindSystemTimeZoneById(ParkTimeZoneId));
    }
}
