namespace Vakantiepark.UnitTests
{
    // Lets tests decide what "now" is, e.g. to check when a lockout ends.
    public class FixedTimeProvider : TimeProvider
    {
        private DateTimeOffset now;

        public FixedTimeProvider(DateTimeOffset now)
        {
            this.now = now;
        }

        public override DateTimeOffset GetUtcNow()
            => now;

        public void Advance(TimeSpan duration)
            => now = now.Add(duration);
    }
}
