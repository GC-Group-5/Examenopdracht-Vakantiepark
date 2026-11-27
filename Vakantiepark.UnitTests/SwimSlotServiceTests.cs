using Moq;
using Vakantiepark.Application.Extensions;
using Vakantiepark.Application.Services;
using Vakantiepark.Domain.Entities;
using Vakantiepark.Domain.Interfaces.Repositories;
using Vakantiepark.Shared.DTOs.Swimming;
using Xunit;

namespace Vakantiepark.UnitTests
{
    public class SwimSlotServiceTests
    {
        // November: the park runs on CET, so 10:00 UTC is 11:00 on the park.
        private static readonly DateTimeOffset UtcNow = new DateTimeOffset(2026, 11, 27, 10, 0, 0, TimeSpan.Zero);
        private static readonly DateOnly Today = new DateOnly(2026, 11, 27);

        private readonly Mock<ISwimSlotRepository> _swimSlotRepository = new Mock<ISwimSlotRepository>();
        private readonly Mock<IBookingRepository> _bookingRepository = new Mock<IBookingRepository>();
        private readonly Mock<IRepository<ParkSettings>> _parkSettingsRepository = new Mock<IRepository<ParkSettings>>();
        private ParkSettings _settings = TestData.CreateParkSettings();
        private readonly SwimSlotService _sut;

        public SwimSlotServiceTests()
        {
            _parkSettingsRepository.Setup(repository => repository.GetAllAsync()).ReturnsAsync(() => [_settings]);
            _swimSlotRepository.Setup(repository => repository.GetByDateAsync(It.IsAny<DateOnly>())).ReturnsAsync([]);
            _swimSlotRepository.Setup(repository => repository.TryAddRangeAsync(It.IsAny<List<SwimSlot>>())).ReturnsAsync(true);

            _sut = new SwimSlotService(_swimSlotRepository.Object, _bookingRepository.Object, _parkSettingsRepository.Object, new FixedTimeProvider(UtcNow));
        }

        // Staying from yesterday until three days from now, with the usual arrival and departure times.
        private Booking SetBooking(Action<Booking>? configure = null)
        {
            Booking booking = TestData.CreateBooking(Today, x =>
            {
                x.ArrivalDate = Today.AddDays(-1);
                x.DepartureDate = Today.AddDays(3);
                x.ArrivalTime = new TimeOnly(15, 0);
                x.DepartureTime = new TimeOnly(10, 0);
                configure?.Invoke(x);
            });
            _bookingRepository.Setup(repository => repository.GetByIdAsync(booking.Id)).ReturnsAsync(booking);
            return booking;
        }

        [Fact]
        public async Task GetByDateAsync_NoSlotsYet_StoresSlotsFromTheSettings()
        {
            Booking booking = SetBooking();

            List<SwimSlotResponse> result = await _sut.GetByDateAsync(booking.Id, Today.AddDays(1));

            Assert.Equal(8, result.Count);
            Assert.Equal(new TimeOnly(9, 0), result[0].StartTime);
            Assert.Equal(new TimeOnly(10, 30), result[0].EndTime);
            Assert.Equal(new TimeOnly(21, 0), result[^1].EndTime);
            Assert.All(result, slot => Assert.Equal(80, slot.RemainingPlaces));
            _swimSlotRepository.Verify(repository => repository.TryAddRangeAsync(It.Is<List<SwimSlot>>(slots => slots.Count == 8)), Times.Once);
        }

        [Fact]
        public async Task GetByDateAsync_SlotsAlreadyStored_KeepsTheirOwnCapacity()
        {
            Booking booking = SetBooking();
            DateOnly date = Today.AddDays(1);
            _swimSlotRepository.Setup(repository => repository.GetByDateAsync(date))
                .ReturnsAsync([new SwimSlot { Date = date, StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(10, 30), Capacity = 40 }]);

            List<SwimSlotResponse> result = await _sut.GetByDateAsync(booking.Id, date);

            Assert.Equal(40, Assert.Single(result).Capacity);
            _swimSlotRepository.Verify(repository => repository.TryAddRangeAsync(It.IsAny<List<SwimSlot>>()), Times.Never);
        }

        [Fact]
        public async Task GetByDateAsync_OtherRequestStoredTheDayFirst_ReturnsTheStoredSlots()
        {
            Booking booking = SetBooking();
            DateOnly date = Today.AddDays(1);
            SwimSlot stored = new SwimSlot { Id = Guid.NewGuid(), Date = date, StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(10, 30), Capacity = 80 };
            _swimSlotRepository.SetupSequence(repository => repository.GetByDateAsync(date))
                .ReturnsAsync([])
                .ReturnsAsync([stored]);
            _swimSlotRepository.Setup(repository => repository.TryAddRangeAsync(It.IsAny<List<SwimSlot>>())).ReturnsAsync(false);

            List<SwimSlotResponse> result = await _sut.GetByDateAsync(booking.Id, date);

            Assert.Equal(stored.Id, Assert.Single(result).Id);
        }

        [Fact]
        public async Task GetByDateAsync_Today_LeavesOutSlotsThatHaveStarted()
        {
            Booking booking = SetBooking();

            List<SwimSlotResponse> result = await _sut.GetByDateAsync(booking.Id, Today);

            Assert.Equal(new TimeOnly(12, 0), result[0].StartTime);
            Assert.Equal(6, result.Count);
        }

        [Fact]
        public async Task GetByDateAsync_ArrivalDay_OnlySlotsFromTheArrivalTime()
        {
            Booking booking = SetBooking(x => x.ArrivalDate = Today.AddDays(1));

            List<SwimSlotResponse> result = await _sut.GetByDateAsync(booking.Id, Today.AddDays(1));

            Assert.Equal(new TimeOnly(15, 0), result[0].StartTime);
        }

        [Fact]
        public async Task GetByDateAsync_DepartureDay_OnlySlotsEndingBeforeTheDepartureTime()
        {
            Booking booking = SetBooking(x => x.DepartureTime = new TimeOnly(11, 0));

            List<SwimSlotResponse> result = await _sut.GetByDateAsync(booking.Id, booking.DepartureDate);

            Assert.Equal(new TimeOnly(10, 30), Assert.Single(result).EndTime);
        }

        [Fact]
        public async Task GetByDateAsync_CapacityZero_IsFull()
        {
            Booking booking = SetBooking();
            _settings = TestData.CreateParkSettings(x => x.SwimSlotDefaultCapacity = 0);

            List<SwimSlotResponse> result = await _sut.GetByDateAsync(booking.Id, Today.AddDays(1));

            Assert.All(result, slot => Assert.True(slot.IsFull));
        }

        [Fact]
        public async Task GetByDateAsync_DateOutsideStay_ThrowsArgumentException()
        {
            Booking booking = SetBooking();

            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() => _sut.GetByDateAsync(booking.Id, booking.DepartureDate.AddDays(1)));

            Assert.Equal("Je kunt alleen zwemmomenten bekijken voor dagen tijdens je verblijf.", exception.Message);
            _swimSlotRepository.Verify(repository => repository.TryAddRangeAsync(It.IsAny<List<SwimSlot>>()), Times.Never);
        }

        [Fact]
        public async Task GetByDateAsync_DayOfTheStayInThePast_ThrowsArgumentException()
        {
            Booking booking = SetBooking();

            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() => _sut.GetByDateAsync(booking.Id, booking.ArrivalDate));

            Assert.Equal("Deze dag is al voorbij. Kies een andere dag.", exception.Message);
        }

        [Fact]
        public async Task GetByDateAsync_CancelledBooking_ThrowsKeyNotFoundException()
        {
            Guid cancelledBookingId = Guid.NewGuid();

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.GetByDateAsync(cancelledBookingId, Today));
        }

        [Fact]
        public async Task GetDaysAsync_StayInProgress_FromTodayAndWithoutDepartureDay()
        {
            Booking booking = SetBooking();

            List<DateOnly> result = await _sut.GetDaysAsync(booking.Id);

            Assert.Equal([Today, Today.AddDays(1), Today.AddDays(2)], result);
        }

        [Fact]
        public async Task GetDaysAsync_LastSlotOfTodayHasStarted_StartsTomorrow()
        {
            Booking booking = SetBooking();
            _settings = TestData.CreateParkSettings(x => x.PoolClosesAt = new TimeOnly(10, 30));

            List<DateOnly> result = await _sut.GetDaysAsync(booking.Id);

            Assert.Equal(Today.AddDays(1), result[0]);
        }

        [Fact]
        public void CreateSwimSlots_SlotWouldRunPastClosingTime_IsLeftOut()
        {
            ParkSettings settings = TestData.CreateParkSettings(x => x.PoolClosesAt = new TimeOnly(20, 0));

            List<SwimSlot> result = settings.CreateSwimSlots(Today);

            Assert.Equal(7, result.Count);
            Assert.Equal(new TimeOnly(19, 30), result[^1].EndTime);
        }
    }
}
