using Moq;
using Vakantiepark.Application.Services;
using Vakantiepark.Domain.Entities;
using Vakantiepark.Domain.Enums;
using Vakantiepark.Domain.Interfaces.Repositories;
using Vakantiepark.Shared.Constants;
using Vakantiepark.Shared.DTOs.Cleaning;
using Xunit;

namespace Vakantiepark.UnitTests
{
    // US-32: the units that become empty on a day, for the cleaner.
    public class CleaningServiceTests
    {
        private static readonly DateOnly Today = new DateOnly(2027, 7, 14);

        private readonly Mock<IBookingRepository> _bookingRepository = new Mock<IBookingRepository>();
        private readonly Mock<ICleaningRepository> _cleaningRepository = new Mock<ICleaningRepository>();
        private readonly CleaningService _sut;
        private DateOnly? _requestedDate;

        public CleaningServiceTests()
        {
            _cleaningRepository
                .Setup(x => x.GetByBookingIdsAsync(It.IsAny<List<Guid>>()))
                .ReturnsAsync(new List<Cleaning>());

            _sut = new CleaningService(_bookingRepository.Object, _cleaningRepository.Object, new FixedTimeProvider(new DateTimeOffset(Today.ToDateTime(new TimeOnly(9, 0)), TimeSpan.Zero)));
        }

        [Fact]
        public async Task GetDayAsync_NoDate_ShowsToday()
        {
            ReturnBookings([]);

            CleaningDayResponse day = await _sut.GetDayAsync(null);

            Assert.Equal(Today, day.Date);
            Assert.Equal(Today, _requestedDate);
        }

        [Fact]
        public async Task GetDayAsync_Tomorrow_ShowsTheDeparturesOfTomorrow()
        {
            ReturnBookings([]);

            CleaningDayResponse day = await _sut.GetDayAsync(Today.AddDays(1));

            Assert.Equal(Today.AddDays(1), day.Date);
            Assert.Equal(Today.AddDays(1), _requestedDate);
        }

        [Fact]
        public async Task GetDayAsync_ArrivalWithoutDeparture_IsNotOnTheList()
        {
            Booking departure = Departure(Unit("BRK4-001"));
            Booking arrivalElsewhere = Arrival(Unit("BRK4-002"));
            ReturnBookings([departure, arrivalElsewhere]);

            CleaningDayResponse day = await _sut.GetDayAsync(null);

            Assert.Equal(departure.Id, Assert.Single(day.Units).BookingId);
        }

        [Fact]
        public async Task GetDayAsync_NewGuestsArriveInTheSameUnit_IsMarked()
        {
            Accommodation changeover = Unit("BRK4-001");
            Accommodation quiet = Unit("BRK4-002");
            ReturnBookings([Departure(changeover), Arrival(changeover), Departure(quiet)]);

            CleaningDayResponse day = await _sut.GetDayAsync(null);

            Assert.True(day.Units.Single(unit => unit.UnitCode == "BRK4-001").GuestsArriveToday);
            Assert.False(day.Units.Single(unit => unit.UnitCode == "BRK4-002").GuestsArriveToday);
        }

        [Fact]
        public async Task GetDayAsync_ShowsCodeTypeAndZoneSortedByZone()
        {
            ReturnBookings([Departure(Unit("BRK4-002", "Waterkant")), Departure(Unit("BRK4-001", "Bosrand"))]);

            CleaningDayResponse day = await _sut.GetDayAsync(null);

            Assert.Equal(new List<string>() { "Bosrand", "Waterkant" }, day.Units.Select(unit => unit.Zone).ToList());
            Assert.Equal("BRK4-001", day.Units[0].UnitCode);
            Assert.Equal("Berk 4", day.Units[0].TypeName);
        }

        [Fact]
        public async Task GetDayAsync_NobodyStarted_IsToClean()
        {
            ReturnBookings([Departure(Unit("BRK4-001"))]);

            CleaningDayResponse day = await _sut.GetDayAsync(null);

            CleaningItemResponse unit = Assert.Single(day.Units);
            Assert.Equal(CleaningStatuses.TeSchoonmaken, unit.Status);
            Assert.Null(unit.CleanedBy);
        }

        [Fact]
        public async Task GetDayAsync_Cleaned_ShowsWhoCleanedItAndWhen()
        {
            Booking departure = Departure(Unit("BRK4-001"));
            DateTime finishedAt = Today.ToDateTime(new TimeOnly(11, 30));
            ReturnBookings([departure]);
            _cleaningRepository
                .Setup(x => x.GetByBookingIdsAsync(It.IsAny<List<Guid>>()))
                .ReturnsAsync(new List<Cleaning>()
                {
                    new Cleaning()
                    {
                        BookingId = departure.Id,
                        Status = CleaningStatus.Clean,
                        FinishedAt = finishedAt,
                        User = TestData.CreateUser(user => user.FullName = "Sam Schoonmaker"),
                    },
                });

            CleaningDayResponse day = await _sut.GetDayAsync(null);

            CleaningItemResponse unit = Assert.Single(day.Units);
            Assert.Equal(CleaningStatuses.Klaar, unit.Status);
            Assert.Equal("Sam Schoonmaker", unit.CleanedBy);
            Assert.Equal(finishedAt, unit.FinishedAt);
        }

        private void ReturnBookings(List<Booking> bookings)
            => _bookingRepository
                .Setup(x => x.GetArrivingOrDepartingOnAsync(It.IsAny<DateOnly>()))
                .Callback<DateOnly>(date => _requestedDate = date)
                .ReturnsAsync(bookings);

        private static Accommodation Unit(string code, string zone = "Waterkant")
            => TestData.CreateAccommodation(code, zone, unit => unit.Type = TestData.CreateAccommodationType(type => type.Name = "Berk 4"));

        private static Booking Departure(Accommodation unit)
            => TestData.CreateBooking(Today, booking =>
            {
                booking.ArrivalDate = Today.AddDays(-4);
                booking.DepartureDate = Today;
                booking.AccommodationUnitId = unit.Id;
                booking.AccommodationUnit = unit;
            });

        private static Booking Arrival(Accommodation unit)
            => TestData.CreateBooking(Today, booking =>
            {
                booking.ArrivalDate = Today;
                booking.DepartureDate = Today.AddDays(5);
                booking.AccommodationUnitId = unit.Id;
                booking.AccommodationUnit = unit;
            });
    }
}
