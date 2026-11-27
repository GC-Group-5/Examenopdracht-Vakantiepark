using Vakantiepark.Domain.Entities;
using Vakantiepark.Domain.Enums;
using Vakantiepark.Shared.Constants;

namespace Vakantiepark.Application.Helpers
{
    // One place that turns a booking into the status text guests and staff see.
    public static class BookingStatus
    {
        // The status is stored since US-46; DeletedAt still wins, because cancelling has always set it.
        public static BookingState GetState(Booking booking)
            => booking.DeletedAt is not null ? BookingState.Cancelled : booking.Status;

        // An unpaid deposit past its due date is shown as its own status, but stays AwaitingDeposit in the
        // database: it only follows from the date, and staff decide whether to cancel (US-46).
        public static string Get(Booking booking, DateOnly today)
            => DepositPolicy.IsLate(booking, today) ? BookingStatuses.AanbetalingTeLaat : ToText(GetState(booking));

        public static string ToText(BookingState state) => state switch
        {
            BookingState.AwaitingDeposit => BookingStatuses.InAfwachting,
            BookingState.CheckedIn => BookingStatuses.Ingecheckt,
            BookingState.CheckedOut => BookingStatuses.Uitgecheckt,
            BookingState.Cancelled => BookingStatuses.Geannuleerd,
            _ => BookingStatuses.Bevestigd,
        };
    }
}
