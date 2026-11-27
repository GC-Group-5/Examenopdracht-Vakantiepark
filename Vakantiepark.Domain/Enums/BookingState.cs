namespace Vakantiepark.Domain.Enums
{
    // AwaitingDeposit and Confirmed are stored; cancellation and completion are derived.
    public enum BookingState
    {
        Confirmed,
        Completed,
        Cancelled,
        AwaitingDeposit,
    }
}
