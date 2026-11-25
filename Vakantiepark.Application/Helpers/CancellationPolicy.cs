using Vakantiepark.Domain.Entities;

namespace Vakantiepark.Application.Helpers
{
    public static class CancellationPolicy
    {
        public static int DaysBeforeArrival(DateOnly arrivalDate, DateOnly today)
            => arrivalDate.DayNumber - today.DayNumber;

        public static bool CanCancel(Booking booking, DateOnly today)
            => booking.DeletedAt is null
                && DaysBeforeArrival(booking.ArrivalDate, today) > 0;

        public static bool CanChange(Booking booking, ParkSettings settings, DateOnly today)
            => booking.DeletedAt is null
                && DaysBeforeArrival(booking.ArrivalDate, today) >= FreeCancellationDays(booking, settings);

        public static decimal CalculateFee(Booking booking, ParkSettings settings, DateOnly today)
        {
            int days = DaysBeforeArrival(booking.ArrivalDate, today);

            if (days > FreeCancellationDays(booking, settings))
                return 0m;

            if (days >= NoRefundDays(booking, settings))
                return booking.Deposit;

            return booking.TotalPrice - booking.TouristTax;
        }

        // The limits agreed when booking (US-18); older bookings without them follow the current settings.
        public static int FreeCancellationDays(Booking booking, ParkSettings settings)
            => booking.FreeCancellationDaysBeforeArrival ?? settings.FreeCancellationDaysBeforeArrival;

        public static int NoRefundDays(Booking booking, ParkSettings settings)
            => booking.NoRefundDaysBeforeArrival ?? settings.NoRefundDaysBeforeArrival;
    }
}
