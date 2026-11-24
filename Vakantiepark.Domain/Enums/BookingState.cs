namespace Vakantiepark.Domain.Enums
{
    // Derived from the booking, never stored: cancelled = DeletedAt is set,
    // completed = departure is in the past, otherwise confirmed.
    public enum BookingState
    {
        Confirmed,
        Completed,
        Cancelled,
    }
}
