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

        public static string Get(Booking booking)
            => ToText(GetState(booking));

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
