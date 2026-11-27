using Vakantiepark.Application.Helpers;
using Vakantiepark.Domain.Entities;
using Vakantiepark.Domain.Enums;
using Xunit;

namespace Vakantiepark.UnitTests
{
    // US-12: more than 30 days free, 30 to 7 days the deposit, fewer than 7 days the stay without tourist tax.
    public class CancellationPolicyTests
    {
        private static readonly DateOnly Today = new DateOnly(2027, 3, 1);
        private readonly ParkSettings _settings = TestData.CreateParkSettings();

        [Theory]
        [InlineData(31)]
        [InlineData(60)]
        public void CalculateFee_MoreThan30DaysBefore_IsFree(int daysBefore)
        {
            Assert.Equal(0m, CancellationPolicy.CalculateFee(CreateBooking(daysBefore), _settings, Today));
        }

        [Theory]
        [InlineData(30)]
        [InlineData(15)]
        [InlineData(7)]
        public void CalculateFee_From30To7DaysBefore_IsTheDeposit(int daysBefore)
        {
            Assert.Equal(120m, CancellationPolicy.CalculateFee(CreateBooking(daysBefore), _settings, Today));
        }

        [Theory]
        [InlineData(6)]
        [InlineData(1)]
        public void CalculateFee_FewerThan7DaysBefore_IsTheStayWithoutTouristTax(int daysBefore)
        {
            Assert.Equal(590m, CancellationPolicy.CalculateFee(CreateBooking(daysBefore), _settings, Today));
        }

        [Fact]
        public void CalculateFee_BookingWithoutStoredLimits_UsesTheParkSettings()
        {
            ParkSettings settings = TestData.CreateParkSettings(s => { s.FreeCancellationDaysBeforeArrival = 60; s.NoRefundDaysBeforeArrival = 14; });

            Assert.Equal(120m, CancellationPolicy.CalculateFee(CreateBooking(45), settings, Today));
            Assert.Equal(590m, CancellationPolicy.CalculateFee(CreateBooking(10), settings, Today));
        }

        // US-18: the beheerder changing the limits must not change what an existing booking agreed to.
        [Fact]
        public void CalculateFee_SettingsChangedAfterBooking_UsesTheLimitsStoredOnTheBooking()
        {
            ParkSettings changedSettings = TestData.CreateParkSettings(s => { s.FreeCancellationDaysBeforeArrival = 60; s.NoRefundDaysBeforeArrival = 14; });
            Booking booking = CreateBooking(45);
            booking.FreeCancellationDaysBeforeArrival = 30;
            booking.NoRefundDaysBeforeArrival = 7;

            Assert.Equal(0m, CancellationPolicy.CalculateFee(booking, changedSettings, Today));
            booking.Status = BookingState.AwaitingDeposit;
            Assert.True(CancellationPolicy.CanChange(booking));
        }

        [Theory]
        [InlineData(1, true)]
        [InlineData(0, false)]
        [InlineData(-3, false)]
        public void CanCancel_OnlyBeforeTheArrivalDay(int daysBefore, bool expected)
        {
            Assert.Equal(expected, CancellationPolicy.CanCancel(CreateBooking(daysBefore), Today));
        }

        [Theory]
        [InlineData(31)]
        [InlineData(29)]
        [InlineData(1)]
        public void CanChange_WhileAwaitingDepositRegardlessOfArrivalDate(int daysBefore)
        {
            Booking booking = CreateBooking(daysBefore);
            booking.Status = BookingState.AwaitingDeposit;

            Assert.True(CancellationPolicy.CanChange(booking));
        }

        [Fact]
        public void CanChange_AfterDepositIsPaid_IsRefused()
        {
            Booking booking = CreateBooking(60);

            Assert.False(CancellationPolicy.CanChange(booking));
        }

        [Fact]
        public void CancelledBooking_CanNotBeCancelledOrChangedAgain()
        {
            Booking booking = CreateBooking(60);
            booking.DeletedAt = DateTime.UtcNow;

            Assert.False(CancellationPolicy.CanCancel(booking, Today));
            booking.Status = BookingState.AwaitingDeposit;
            Assert.False(CancellationPolicy.CanChange(booking));
        }

        // Total 600: stay 480 + cleaning 110 + tourist tax 10; deposit 120 (20%).
        private static Booking CreateBooking(int daysBeforeArrival)
            => new Booking()
            {
                ArrivalDate = Today.AddDays(daysBeforeArrival),
                DepartureDate = Today.AddDays(daysBeforeArrival + 4),
                AccommodationPrice = 480m,
                CleaningFee = 110m,
                TouristTax = 10m,
                TotalPrice = 600m,
                Deposit = 120m,
            };
    }
}
