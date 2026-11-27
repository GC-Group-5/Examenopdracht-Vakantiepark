using Vakantiepark.Domain.Entities;
using Vakantiepark.Domain.Enums;

namespace Vakantiepark.Application.Helpers
{
    // US-46 (client review of 26 November): who books within the free-cancellation term (30 days) pays the
    // deposit at once; who books earlier pays it a set number of days (7) before arrival. Both are park settings.
    public static class DepositPolicy
    {
        public static DateOnly DueDate(DateOnly arrivalDate, int freeCancellationDaysBeforeArrival, int depositDueDaysBeforeArrival, DateOnly bookedOn)
        {
            if (arrivalDate.AddDays(-freeCancellationDaysBeforeArrival) <= bookedOn)
                return bookedOn;

            // Never before the booking date, also when the deposit term is set longer than the cancellation term.
            DateOnly dueDate = arrivalDate.AddDays(-depositDueDaysBeforeArrival);
            return dueDate < bookedOn ? bookedOn : dueDate;
        }

        // Paying on the due date itself is still on time; the same rule as the "Aanbetaling te laat" filter.
        public static bool IsLate(Booking booking, DateOnly today)
            => booking.Status == BookingState.AwaitingDeposit && booking.DeletedAt is null && booking.DepositDueDate < today;
    }
}
