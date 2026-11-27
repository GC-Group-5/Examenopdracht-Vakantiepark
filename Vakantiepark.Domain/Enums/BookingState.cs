namespace Vakantiepark.Domain.Enums
{
    // Stored on the booking (US-46). Cancelled also sets DeletedAt, so the overlap check keeps working.
    public enum BookingState
    {
        AwaitingDeposit,
        Confirmed,
        CheckedIn,
        CheckedOut,
        Cancelled,
    }
}
