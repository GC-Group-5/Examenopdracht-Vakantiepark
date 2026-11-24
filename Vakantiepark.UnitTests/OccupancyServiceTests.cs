using Moq;
using Vakantiepark.Application.Services;
using Vakantiepark.Domain.Entities;
using Vakantiepark.Domain.Enums;
using Vakantiepark.Domain.Interfaces.Repositories;
using Vakantiepark.Shared.Constants;
using Vakantiepark.Shared.DTOs.Occupancy;
using Xunit;

namespace Vakantiepark.UnitTests
{
    // US-16: the status of every unit on one date for staff.
    public class OccupancyServiceTests
    {
        private static readonly DateOnly Today = new DateOnly(2027, 7, 14);

        private readonly Mock<IAccommodationUnitRepository> _unitRepository = new Mock<IAccommodationUnitRepository>();
        private readonly Mock<IBookingRepository> _bookingRepository = new Mock<IBookingRepository>();
        private readonly OccupancyService _sut;

        public OccupancyServiceTests()
        {
            _sut = new OccupancyService(_unitRepository.Object, _bookingRepository.Object, new FixedTimeProvider(new DateTimeOffset(Today.ToDateTime(new TimeOnly(9, 0)), TimeSpan.Zero)));
        }

        [Fact]
        public async Task GetAsync_GivesEveryUnitItsStatusAndCountsThem()
        {
            Accommodation free = CreateUnit("BRK4-001");
            Accommodation booked = CreateUnit("BRK4-002");
            Accommodation inMaintenance = CreateUnit("BRK4-003");
            inMaintenance.MaintenancePeriods = [new MaintenancePeriod() { StartDate = Today, EndDate = Today.AddDays(3), Reason = MaintenanceReason.Maintenance }];
            Booking booking = TestData.CreateBooking(Today, booking => booking.AccommodationUnitId = booked.Id);
            Setup([free, booked, inMaintenance], [booking]);

            OccupancyResponse occupancy = await _sut.GetAsync(new OccupancyRequest());

            Assert.Equal(Today, occupancy.Date);
            Assert.Equal(new List<string>() { OccupancyStatuses.Beschikbaar, OccupancyStatuses.Bezet, OccupancyStatuses.NietVerhuurbaar }, occupancy.Units.Items.Select(unit => unit.Status).ToList());
            Assert.Equal(booking.Id, occupancy.Units.Items[1].BookingId);
            Assert.Equal("Maintenance", occupancy.Units.Items[2].MaintenanceReason);
            Assert.Equal(1, occupancy.AvailableCount);
            Assert.Equal(1, occupancy.OccupiedCount);
            Assert.Equal(1, occupancy.OutOfServiceCount);
        }

        [Fact]
        public async Task GetAsync_UnitInMaintenanceWithABooking_IsOutOfServiceAndKeepsTheBooking()
        {
            Accommodation unit = CreateUnit("BRK4-001");
            unit.MaintenancePeriods = [new MaintenancePeriod() { StartDate = Today, EndDate = Today, Reason = MaintenanceReason.TechnicalProblem }];
            Booking booking = TestData.CreateBooking(Today, booking => booking.AccommodationUnitId = unit.Id);
            Setup([unit], [booking]);

            OccupancyUnitResponse row = Assert.Single((await _sut.GetAsync(new OccupancyRequest())).Units.Items);

            Assert.Equal(OccupancyStatuses.NietVerhuurbaar, row.Status);
            Assert.Equal(booking.Id, row.BookingId);
        }

        [Fact]
        public async Task GetAsync_Paging_CountsAllUnitsButReturnsOnePage()
        {
            Setup(Enumerable.Range(1, 5).Select(number => CreateUnit($"BRK4-00{number}")).ToList(), []);

            OccupancyResponse occupancy = await _sut.GetAsync(new OccupancyRequest() { Page = 2, PageSize = 2 });

            Assert.Equal(new List<string>() { "BRK4-003", "BRK4-004" }, occupancy.Units.Items.Select(unit => unit.Code).ToList());
            Assert.Equal(5, occupancy.Units.TotalCount);
            Assert.Equal(5, occupancy.AvailableCount);
        }

        [Fact]
        public async Task GetAsync_UnknownCategory_Throws()
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _sut.GetAsync(new OccupancyRequest() { Category = "Bungalow" }));
        }

        private void Setup(List<Accommodation> units, List<Booking> bookings)
        {
            _unitRepository
                .Setup(x => x.GetForOccupancyAsync(Today, It.IsAny<string?>(), It.IsAny<Guid?>(), It.IsAny<AccommodationCategory?>()))
                .ReturnsAsync(units);
            _bookingRepository
                .Setup(x => x.GetStayingOnDateAsync(It.IsAny<List<Guid>>(), Today))
                .ReturnsAsync(bookings);
        }

        private static Accommodation CreateUnit(string code)
            => TestData.CreateAccommodation(code, configure: unit => unit.Type = TestData.CreateAccommodationType());
    }
}
