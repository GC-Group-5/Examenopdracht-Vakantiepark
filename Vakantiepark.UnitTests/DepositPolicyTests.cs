using Vakantiepark.Application.Helpers;
using Xunit;

namespace Vakantiepark.UnitTests
{
    // Free cancellation until 30 days before arrival, deposit due 7 days before arrival.
    public class DepositPolicyTests
    {
        private const int FreeCancellationDays = 30;
        private const int DepositDueDays = 7;
        private static readonly DateOnly BookedOn = new DateOnly(2027, 3, 1);

        [Fact]
        public void DueDate_BookedMoreThanThirtyDaysAhead_IsSevenDaysBeforeArrival()
        {
            DateOnly dueDate = DepositPolicy.DueDate(new DateOnly(2027, 6, 1), FreeCancellationDays, DepositDueDays, BookedOn);

            Assert.Equal(new DateOnly(2027, 5, 25), dueDate);
        }

        [Fact]
        public void DueDate_BookedWithinThirtyDays_IsTheBookingDate()
        {
            DateOnly dueDate = DepositPolicy.DueDate(BookedOn.AddDays(10), FreeCancellationDays, DepositDueDays, BookedOn);

            Assert.Equal(BookedOn, dueDate);
        }

        [Fact]
        public void DueDate_BookedExactlyThirtyDaysAhead_IsTheBookingDate()
        {
            DateOnly dueDate = DepositPolicy.DueDate(BookedOn.AddDays(30), FreeCancellationDays, DepositDueDays, BookedOn);

            Assert.Equal(BookedOn, dueDate);
        }

        [Fact]
        public void DueDate_DepositTermLongerThanTheBookingIsAhead_IsNeverBeforeTheBookingDate()
        {
            DateOnly dueDate = DepositPolicy.DueDate(BookedOn.AddDays(35), FreeCancellationDays, depositDueDaysBeforeArrival: 40, BookedOn);

            Assert.Equal(BookedOn, dueDate);
        }
    }
}
