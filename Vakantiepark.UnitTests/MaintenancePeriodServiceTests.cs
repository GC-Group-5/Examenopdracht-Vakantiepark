using Moq;
using Vakantiepark.Application.Interfaces;
using Vakantiepark.Application.Services;
using Vakantiepark.Domain.Entities;
using Vakantiepark.Domain.Enums;
using Vakantiepark.Domain.Interfaces.Repositories;
using Vakantiepark.Shared.DTOs.Maintenance;
using Xunit;

namespace Vakantiepark.UnitTests
{
    // US-17: staff registers a maintenance period per unit; overlapping bookings need
    // explicit confirmation, and a period can only be shortened.
    public class MaintenancePeriodServiceTests
    {
        private readonly Mock<IRepository<MaintenancePeriod>> _periodRepository = new();
        private readonly Mock<IAccommodationUnitRepository> _unitRepository = new();
        private readonly Mock<IBookingRepository> _bookingRepository = new();
        private readonly MaintenancePeriodService _sut;

        private readonly Accommodation _unit = new() { Id = Guid.NewGuid(), Code = "BERK-A", Name = "Berk 4 personen A" };
        private readonly MaintenancePeriod _period = new()
        {
            Id = Guid.NewGuid(),
            AccommodationUnitId = Guid.Empty,
            StartDate = new DateOnly(2027, 3, 1),
            EndDate = new DateOnly(2027, 3, 10),
            Reason = MaintenanceReason.Maintenance,
            Note = "Loodgieterswerk",
        };

        private static readonly DateOnly Start = new DateOnly(2027, 3, 1);
        private static readonly DateOnly End = new DateOnly(2027, 3, 10);

        public MaintenancePeriodServiceTests()
        {
            _period.AccommodationUnitId = _unit.Id;

            _unitRepository.Setup(x => x.GetByCodeWithDetailsAsync("BERK-A")).ReturnsAsync(_unit);
            _unitRepository.Setup(x => x.GetByCodeWithDetailsAsync("BESTAATNIET")).ReturnsAsync((Accommodation?)null);
            _periodRepository.Setup(x => x.GetAllAsync()).ReturnsAsync([_period]);
            _periodRepository.Setup(x => x.GetByIdAsync(_period.Id)).ReturnsAsync(_period);
            _periodRepository.Setup(x => x.AddAsync(It.IsAny<MaintenancePeriod>())).ReturnsAsync((MaintenancePeriod p) => p);
            _bookingRepository
                .Setup(x => x.GetByUnitIdInPeriodAsync(_unit.Id, It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
                .ReturnsAsync([]);

            _sut = new MaintenancePeriodService(_periodRepository.Object, _unitRepository.Object, _bookingRepository.Object);
        }

        [Fact]
        public async Task CreateAsync_NoBookings_PeriodIsRegistered()
        {
            CreateMaintenancePeriodResult result = await _sut.CreateAsync(NewRequest(Start, End, "Maintenance"));

            Assert.True(result.Created);
            Assert.False(result.RequiresConfirmation);
            Assert.Null(result.Message);
            _periodRepository.Verify(x => x.AddAsync(It.IsAny<MaintenancePeriod>()), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_OverlappingBookingsWithoutConfirmation_NotCreatedAndConflictsListed()
        {
            SetupBookings(new Booking
            {
                Id = Guid.NewGuid(),
                BookingNumber = "VP-2027-001",
                Name = "Jansen",
                ArrivalDate = new DateOnly(2027, 3, 5),
                DepartureDate = new DateOnly(2027, 3, 9),
                NumberOfGuests = 4,
            });

            CreateMaintenancePeriodResult result = await _sut.CreateAsync(NewRequest(Start, End, "Maintenance"));

            Assert.False(result.Created);
            Assert.True(result.RequiresConfirmation);
            BookingConflictResponse conflict = Assert.Single(result.ConflictingBookings);
            Assert.Equal("VP-2027-001", conflict.BookingNumber);
            Assert.Contains("actie nodig", result.Message);
            _periodRepository.Verify(x => x.AddAsync(It.IsAny<MaintenancePeriod>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_OverlappingBookingsConfirmed_PeriodIsRegisteredWithConflicts()
        {
            SetupBookings(new Booking
            {
                Id = Guid.NewGuid(),
                BookingNumber = "VP-2027-001",
                Name = "Jansen",
                ArrivalDate = new DateOnly(2027, 3, 5),
                DepartureDate = new DateOnly(2027, 3, 9),
                NumberOfGuests = 4,
            });

            CreateMaintenancePeriodResult result = await _sut.CreateAsync(NewRequest(Start, End, "Maintenance", confirmed: true));

            Assert.True(result.Created);
            Assert.Single(result.ConflictingBookings);
            _periodRepository.Verify(x => x.AddAsync(It.IsAny<MaintenancePeriod>()), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_UnknownUnit_ThrowsArgumentException()
        {
            CreateMaintenancePeriodRequest request = NewRequest(Start, End, "Maintenance");
            request.AccommodationUnitCode = "BESTAATNIET";

            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() => _sut.CreateAsync(request));
            Assert.Contains("is niet gevonden", exception.Message);
        }

        [Fact]
        public async Task CreateAsync_StartInPast_ThrowsArgumentException()
        {
            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _sut.CreateAsync(NewRequest(new DateOnly(2020, 1, 5), new DateOnly(2020, 1, 10), "Maintenance")));

            Assert.Contains("mag niet in het verleden liggen", exception.Message);
        }

        [Fact]
        public async Task CreateAsync_EndBeforeStart_ThrowsArgumentException()
        {
            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _sut.CreateAsync(NewRequest(End, Start, "Maintenance")));

            Assert.Contains("einddatum moet op of na de begindatum liggen", exception.Message);
        }

        [Fact]
        public async Task CreateAsync_UnknownReason_ThrowsArgumentException()
        {
            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _sut.CreateAsync(NewRequest(Start, End, "Slopen")));

            Assert.Contains("Kies onderhoud, technisch probleem of overig als reden.", exception.Message);
        }

        [Fact]
        public async Task CreateAsync_ReasonOtherWithoutNote_ThrowsArgumentException()
        {
            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _sut.CreateAsync(NewRequest(Start, End, "Other")));

            Assert.Contains("toelichting", exception.Message);
        }

        [Fact]
        public async Task CreateAsync_ReasonOtherWithNote_IsRegistered()
        {
            CreateMaintenancePeriodResult result = await _sut.CreateAsync(
                NewRequest(Start, End, "Other", note: "Schoonmaakploeg staat hier geparkeerd."));

            Assert.True(result.Created);
            Assert.Equal("Schoonmaakploeg staat hier geparkeerd.", result.Period?.Note);
        }

        [Fact]
        public async Task UpdateAsync_ShorteningPeriod_UpdatesDates()
        {
            MaintenancePeriodResponse response = await _sut.UpdateAsync(_period.Id, new UpdateMaintenancePeriodRequest
            {
                StartDate = new DateOnly(2027, 3, 4),
                EndDate = new DateOnly(2027, 3, 5),
            });

            Assert.Equal(new DateOnly(2027, 3, 4), response.StartDate);
            Assert.Equal(new DateOnly(2027, 3, 5), response.EndDate);
            _periodRepository.Verify(x => x.UpdateAsync(_period), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_EndingPeriodToday_KeepsStartOfOriginalPeriod()
        {
            MaintenancePeriodResponse response = await _sut.UpdateAsync(_period.Id, new UpdateMaintenancePeriodRequest
            {
                StartDate = _period.StartDate,
                EndDate = _period.StartDate,
            });

            Assert.Equal(_period.StartDate, response.EndDate);
        }

        // A period that is already running (started 3 days ago) is ended today because the
        // unit is ready early (criterion 4). Its start date is in the past, which is fine here.
        [Fact]
        public async Task UpdateAsync_OngoingPeriodEndedToday_UpdatesEndDate()
        {
            DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
            _period.StartDate = today.AddDays(-3);
            _period.EndDate = today.AddDays(5);

            MaintenancePeriodResponse response = await _sut.UpdateAsync(_period.Id, new UpdateMaintenancePeriodRequest
            {
                StartDate = _period.StartDate,
                EndDate = today,
            });

            Assert.Equal(today, response.EndDate);
            _periodRepository.Verify(x => x.UpdateAsync(_period), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_NewEndDateInPast_ThrowsArgumentException()
        {
            DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
            _period.StartDate = today.AddDays(-5);
            _period.EndDate = today.AddDays(5);

            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _sut.UpdateAsync(_period.Id, new UpdateMaintenancePeriodRequest
                {
                    StartDate = _period.StartDate,
                    EndDate = today.AddDays(-1),
                }));

            Assert.Equal("Een periode kan niet in het verleden eindigen.", exception.Message);
        }

        [Fact]
        public async Task UpdateAsync_WideningPeriod_ThrowsArgumentException()
        {
            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _sut.UpdateAsync(_period.Id, new UpdateMaintenancePeriodRequest
                {
                    StartDate = new DateOnly(2027, 2, 20),
                    EndDate = new DateOnly(2027, 3, 12),
                }));

            Assert.Contains("alleen worden ingekort", exception.Message);
        }

        [Fact]
        public async Task UpdateAsync_UnknownPeriod_ThrowsArgumentException()
        {
            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _sut.UpdateAsync(Guid.NewGuid(), new UpdateMaintenancePeriodRequest
                {
                    StartDate = Start,
                    EndDate = End,
                }));

            Assert.Contains("niet gevonden", exception.Message);
        }

        [Fact]
        public async Task GetOverviewAsync_ListsPeriodsWithConflictingBookings()
        {
            SetupBookings(new Booking
            {
                Id = Guid.NewGuid(),
                BookingNumber = "VP-2027-001",
                Name = "Jansen",
                ArrivalDate = new DateOnly(2027, 3, 5),
                DepartureDate = new DateOnly(2027, 3, 9),
                NumberOfGuests = 4,
            });

            MaintenanceUnitOverviewResponse overview = await _sut.GetOverviewAsync("BERK-A");

            Assert.Equal("BERK-A", overview.UnitCode);
            MaintenancePeriodResponse period = Assert.Single(overview.Periods);
            Assert.Equal("Maintenance", period.Reason);
            BookingConflictResponse conflict = Assert.Single(period.ConflictingBookings);
            Assert.Equal("VP-2027-001", conflict.BookingNumber);
        }

        [Fact]
        public async Task GetOverviewAsync_UnknownUnit_ThrowsArgumentException()
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _sut.GetOverviewAsync("BESTAATNIET"));
        }

        private void SetupBookings(Booking booking)
            => _bookingRepository
                .Setup(x => x.GetByUnitIdInPeriodAsync(_unit.Id, It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
                .ReturnsAsync([booking]);

        private static CreateMaintenancePeriodRequest NewRequest(DateOnly start, DateOnly end, string reason, bool confirmed = false, string? note = null)
            => new()
            {
                AccommodationUnitCode = "BERK-A",
                StartDate = start,
                EndDate = end,
                Reason = reason,
                Note = note,
                ConfirmExistingBookings = confirmed,
            };
    }
}
