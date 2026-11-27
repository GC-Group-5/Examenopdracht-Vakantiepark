using Vakantiepark.Domain.Entities;
using Vakantiepark.Domain.Enums;
using Vakantiepark.Shared.Constants;

namespace Vakantiepark.Application.Helpers
{
    // One place that turns a booking into the status text guests and staff see.
    public static class BookingStatus
    {
        public static BookingState GetState(Booking booking, DateOnly today)
        {
            if (booking.DeletedAt is not null)
                return BookingState.Cancelled;

            if (booking.DepartureDate < today)
                return BookingState.Completed;

            return booking.Status;
        }

        public static string Get(Booking booking, DateOnly today)
            => ToText(GetState(booking, today));

        public static string ToText(BookingState state) => state switch
        {
            BookingState.Cancelled => BookingStatuses.Geannuleerd,
            BookingState.Completed => BookingStatuses.Afgerond,
            BookingState.AwaitingDeposit => BookingStatuses.InAfwachtingVanAanbetaling,
            _ => BookingStatuses.Bevestigd,
        };
    }
}
