namespace Vakantiepark.Application.Extensions
{
    public static class TimeProviderExtensions
    {
        // Stays are whole days without a time zone, so every rule compares against the same UTC date.
        public static DateOnly GetToday(this TimeProvider timeProvider)
            => DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
    }
}
