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
        private static readonly DateTime Now = Today.ToDateTime(new TimeOnly(9, 0));
        private static readonly Guid CleanerId = Guid.NewGuid();

        private readonly Mock<IBookingRepository> _bookingRepository = new Mock<IBookingRepository>();
        private readonly Mock<ICleaningRepository> _cleaningRepository = new Mock<ICleaningRepository>();
        private readonly CleaningService _sut;
        private DateOnly? _requestedDate;

        public CleaningServiceTests()
        {
            _cleaningRepository
                .Setup(x => x.GetByBookingIdsAsync(It.IsAny<List<Guid>>()))
                .ReturnsAsync(new List<Cleaning>());

            _sut = new CleaningService(_bookingRepository.Object, _cleaningRepository.Object, new FixedTimeProvider(new DateTimeOffset(Now, TimeSpan.Zero)));
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

        [Fact]
        public async Task StartAsync_UnitToClean_IsInProgressWithCleaner()
        {
            Booking departure = ReturnBooking(Departure(Unit("BRK4-001")));
            Cleaning? added = null;
            _cleaningRepository
                .Setup(x => x.AddAsync(It.IsAny<Cleaning>()))
                .Callback<Cleaning>(cleaning => added = cleaning)
                .ReturnsAsync((Cleaning cleaning) => cleaning);

            await _sut.StartAsync(departure.Id, CleanerId);

            Assert.NotNull(added);
            Assert.Equal(CleaningStatus.InProgress, added.Status);
            Assert.Equal(CleanerId, added.UserId);
            Assert.Equal(Now, added.StartedAt);
            Assert.Equal(departure.AccommodationUnitId, added.AccommodationUnitId);
        }

        [Fact]
        public async Task StartAsync_GuestsLeaveTomorrow_Throws()
        {
            Booking departure = ReturnBooking(Departure(Unit("BRK4-001")));
            departure.DepartureDate = Today.AddDays(1);

            await Assert.ThrowsAsync<ArgumentException>(() => _sut.StartAsync(departure.Id, CleanerId));
        }

        [Fact]
        public async Task StartAsync_SomeoneElseIsBusy_Throws()
        {
            Booking departure = ReturnBooking(Departure(Unit("BRK4-001")));
            ReturnCleaning(departure, CleaningStatus.InProgress);

            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() => _sut.StartAsync(departure.Id, CleanerId));

            Assert.Contains("Sam Schoonmaker", exception.Message);
        }

        [Fact]
        public async Task StartAsync_AlreadyClean_Throws()
        {
            Booking departure = ReturnBooking(Departure(Unit("BRK4-001")));
            ReturnCleaning(departure, CleaningStatus.Clean);

            await Assert.ThrowsAsync<ArgumentException>(() => _sut.StartAsync(departure.Id, CleanerId));
        }

        [Fact]
        public async Task StartAsync_UnknownOrCancelledBooking_ThrowsNotFound()
        {
            await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.StartAsync(Guid.NewGuid(), CleanerId));
        }

        [Fact]
        public async Task FinishAsync_InProgress_IsCleanWithCleanerAndTime()
        {
            Booking departure = ReturnBooking(Departure(Unit("BRK4-001")));
            Cleaning cleaning = ReturnCleaning(departure, CleaningStatus.InProgress);

            await _sut.FinishAsync(departure.Id, CleanerId);

            Assert.Equal(CleaningStatus.Clean, cleaning.Status);
            Assert.Equal(CleanerId, cleaning.UserId);
            Assert.Equal(Now, cleaning.FinishedAt);
            _cleaningRepository.Verify(x => x.UpdateAsync(cleaning), Times.Once);
        }

        [Fact]
        public async Task FinishAsync_NotStarted_Throws()
        {
            Booking departure = ReturnBooking(Departure(Unit("BRK4-001")));

            await Assert.ThrowsAsync<ArgumentException>(() => _sut.FinishAsync(departure.Id, CleanerId));
        }

        private Booking ReturnBooking(Booking booking)
        {
            _bookingRepository.Setup(x => x.GetByIdAsync(booking.Id)).ReturnsAsync(booking);
            return booking;
        }

        private Cleaning ReturnCleaning(Booking booking, CleaningStatus status)
        {
            Cleaning cleaning = new Cleaning()
            {
                BookingId = booking.Id,
                Status = status,
                UserId = Guid.NewGuid(),
                User = TestData.CreateUser(user => user.FullName = "Sam Schoonmaker"),
            };
            _cleaningRepository.Setup(x => x.GetByBookingIdAsync(booking.Id)).ReturnsAsync(cleaning);
            return cleaning;
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
