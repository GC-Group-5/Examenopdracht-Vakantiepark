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
				&& DaysBeforeArrival(booking.ArrivalDate, today) >= settings.FreeCancellationDaysBeforeArrival;

		public static decimal CalculateFee(Booking booking, ParkSettings settings, DateOnly today)
		{
			int days = DaysBeforeArrival(booking.ArrivalDate, today);

			if (days > settings.FreeCancellationDaysBeforeArrival)
				return 0m;

			if (days >= settings.NoRefundDaysBeforeArrival)
				return booking.Deposit;

			return booking.TotalPrice - booking.TouristTax;
		}
	}
}