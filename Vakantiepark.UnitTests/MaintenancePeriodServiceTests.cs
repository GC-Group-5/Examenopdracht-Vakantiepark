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
    // explicit confirmation; a period can be shortened, extended or ended.
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

        // A range that doesn't overlap the stored period, for registering a new one.
        private static readonly DateOnly FreeStart = new DateOnly(2027, 4, 1);
        private static readonly DateOnly FreeEnd = new DateOnly(2027, 4, 10);

        // Fixed "now" so the 2027 fixtures stay in the future, whatever the real date is.
        private static readonly DateOnly Today = new DateOnly(2027, 2, 15);
        private readonly FixedTimeProvider _timeProvider = new FixedTimeProvider(new DateTimeOffset(2027, 2, 15, 12, 0, 0, TimeSpan.Zero));

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

            _sut = new MaintenancePeriodService(_periodRepository.Object, _unitRepository.Object, _bookingRepository.Object, _timeProvider);
        }

        [Fact]
        public async Task CreateAsync_NoBookings_PeriodIsRegistered()
        {
            MaintenancePeriodSaveResult result = await _sut.CreateAsync(NewRequest(FreeStart, FreeEnd, "Maintenance"));

            Assert.True(result.Saved);
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

            MaintenancePeriodSaveResult result = await _sut.CreateAsync(NewRequest(FreeStart, FreeEnd, "Maintenance"));

            Assert.False(result.Saved);
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

            MaintenancePeriodSaveResult result = await _sut.CreateAsync(NewRequest(FreeStart, FreeEnd, "Maintenance", confirmed: true));

            Assert.True(result.Saved);
            Assert.Single(result.ConflictingBookings);
            _periodRepository.Verify(x => x.AddAsync(It.IsAny<MaintenancePeriod>()), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_UnknownUnit_ThrowsArgumentException()
        {
            CreateMaintenancePeriodRequest request = NewRequest(FreeStart, FreeEnd, "Maintenance");
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
                () => _sut.CreateAsync(NewRequest(FreeStart, FreeEnd, "Slopen")));

            Assert.Contains("Kies onderhoud, technisch probleem of overig als reden.", exception.Message);
        }

        [Fact]
        public async Task CreateAsync_ReasonOtherWithoutNote_ThrowsArgumentException()
        {
            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _sut.CreateAsync(NewRequest(FreeStart, FreeEnd, "Other")));

            Assert.Contains("toelichting", exception.Message);
        }

        [Fact]
        public async Task CreateAsync_ReasonOtherWithNote_IsRegistered()
        {
            MaintenancePeriodSaveResult result = await _sut.CreateAsync(
                NewRequest(FreeStart, FreeEnd, "Other", note: "Schoonmaakploeg staat hier geparkeerd."));

            Assert.True(result.Saved);
            Assert.Equal("Schoonmaakploeg staat hier geparkeerd.", result.Period?.Note);
        }

        [Fact]
        public async Task UpdateAsync_ShorteningPeriod_UpdatesDates()
        {
            MaintenancePeriodSaveResult result = await _sut.UpdateAsync(_period.Id, new UpdateMaintenancePeriodRequest
            {
                StartDate = new DateOnly(2027, 3, 4),
                EndDate = new DateOnly(2027, 3, 5),
            });

            Assert.True(result.Saved);
            Assert.Equal(new DateOnly(2027, 3, 4), result.Period?.StartDate);
            Assert.Equal(new DateOnly(2027, 3, 5), result.Period?.EndDate);
            _periodRepository.Verify(x => x.UpdateAsync(_period), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_EndingOnFirstDay_KeepsStartOfOriginalPeriod()
        {
            MaintenancePeriodSaveResult result = await _sut.UpdateAsync(_period.Id, new UpdateMaintenancePeriodRequest
            {
                StartDate = _period.StartDate,
                EndDate = _period.StartDate,
            });

            Assert.Equal(_period.StartDate, result.Period?.EndDate);
        }

        // A period that is already running (started 3 days ago) is ended today because the
        // unit is ready early (criterion 4). Its start date is in the past, which is fine here.
        [Fact]
        public async Task UpdateAsync_OngoingPeriodEndedToday_UpdatesEndDate()
        {
            _period.StartDate = Today.AddDays(-3);
            _period.EndDate = Today.AddDays(5);

            MaintenancePeriodSaveResult result = await _sut.UpdateAsync(_period.Id, new UpdateMaintenancePeriodRequest
            {
                StartDate = _period.StartDate,
                EndDate = Today,
            });

            Assert.Equal(Today, result.Period?.EndDate);
            _periodRepository.Verify(x => x.UpdateAsync(_period), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_NewEndDateInPast_ThrowsArgumentException()
        {
            _period.StartDate = Today.AddDays(-5);
            _period.EndDate = Today.AddDays(5);

            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _sut.UpdateAsync(_period.Id, new UpdateMaintenancePeriodRequest
                {
                    StartDate = _period.StartDate,
                    EndDate = Today.AddDays(-1),
                }));

            Assert.Equal("Een periode kan niet in het verleden eindigen. De huidige datum is 15-02-2027.", exception.Message);
        }

        // Staff needs more time: the end date moves later and no booking falls in the added days.
        [Fact]
        public async Task UpdateAsync_ExtendingPeriodWithoutNewBookings_UpdatesDates()
        {
            MaintenancePeriodSaveResult result = await _sut.UpdateAsync(_period.Id, new UpdateMaintenancePeriodRequest
            {
                StartDate = _period.StartDate,
                EndDate = new DateOnly(2027, 3, 15),
            });

            Assert.True(result.Saved);
            Assert.Equal(new DateOnly(2027, 3, 15), result.Period?.EndDate);
            _periodRepository.Verify(x => x.UpdateAsync(_period), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ExtendingOverNewBookingWithoutConfirmation_NotSavedAndBookingListed()
        {
            SetupBookingInExtension();

            MaintenancePeriodSaveResult result = await _sut.UpdateAsync(_period.Id, new UpdateMaintenancePeriodRequest
            {
                StartDate = _period.StartDate,
                EndDate = new DateOnly(2027, 3, 15),
            });

            Assert.False(result.Saved);
            Assert.True(result.RequiresConfirmation);
            Assert.Equal("VP-2027-002", Assert.Single(result.ConflictingBookings).BookingNumber);
            _periodRepository.Verify(x => x.UpdateAsync(It.IsAny<MaintenancePeriod>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_ExtendingOverNewBookingConfirmed_IsSaved()
        {
            SetupBookingInExtension();

            MaintenancePeriodSaveResult result = await _sut.UpdateAsync(_period.Id, new UpdateMaintenancePeriodRequest
            {
                StartDate = _period.StartDate,
                EndDate = new DateOnly(2027, 3, 15),
                ConfirmExistingBookings = true,
            });

            Assert.True(result.Saved);
            _periodRepository.Verify(x => x.UpdateAsync(_period), Times.Once);
        }

        // Bookings inside the old dates were confirmed when the period was registered.
        [Fact]
        public async Task UpdateAsync_ShorteningWithAlreadyConfirmedBooking_NeedsNoNewConfirmation()
        {
            SetupBookings(new Booking { Id = Guid.NewGuid(), BookingNumber = "VP-2027-001", ArrivalDate = new DateOnly(2027, 3, 2), DepartureDate = new DateOnly(2027, 3, 4) });

            MaintenancePeriodSaveResult result = await _sut.UpdateAsync(_period.Id, new UpdateMaintenancePeriodRequest
            {
                StartDate = _period.StartDate,
                EndDate = new DateOnly(2027, 3, 5),
            });

            Assert.True(result.Saved);
        }

        [Fact]
        public async Task UpdateAsync_MovingStartOfRunningPeriod_ThrowsArgumentException()
        {
            _period.StartDate = Today.AddDays(-3);
            _period.EndDate = Today.AddDays(5);

            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _sut.UpdateAsync(_period.Id, new UpdateMaintenancePeriodRequest
                {
                    StartDate = Today.AddDays(-5),
                    EndDate = _period.EndDate,
                }));

            Assert.Contains("lopende periode", exception.Message);
        }

        [Fact]
        public async Task UpdateAsync_PlannedPeriodMovedIntoThePast_ThrowsArgumentException()
        {
            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _sut.UpdateAsync(_period.Id, new UpdateMaintenancePeriodRequest
                {
                    StartDate = Today.AddDays(-1),
                    EndDate = _period.EndDate,
                }));

            Assert.Contains("mag niet in het verleden liggen", exception.Message);
        }

        [Fact]
        public async Task UpdateAsync_EndedPeriod_ThrowsArgumentException()
        {
            _period.StartDate = Today.AddDays(-10);
            _period.EndDate = Today.AddDays(-2);

            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _sut.UpdateAsync(_period.Id, new UpdateMaintenancePeriodRequest
                {
                    StartDate = _period.StartDate,
                    EndDate = Today.AddDays(3),
                }));

            Assert.Contains("afgelopen periode", exception.Message);
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
        public async Task CreateAsync_OverlapsExistingPeriodOfUnit_ThrowsArgumentException()
        {
            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _sut.CreateAsync(NewRequest(new DateOnly(2027, 3, 8), new DateOnly(2027, 3, 12), "Maintenance")));

            Assert.Contains("heeft al een onderhoudsperiode van 01-03-2027 t/m 10-03-2027", exception.Message);
            _periodRepository.Verify(x => x.AddAsync(It.IsAny<MaintenancePeriod>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_ExtendingIntoOtherPeriodOfUnit_ThrowsArgumentException()
        {
            MaintenancePeriod next = new() { Id = Guid.NewGuid(), AccommodationUnitId = _unit.Id, StartDate = new DateOnly(2027, 3, 14), EndDate = new DateOnly(2027, 3, 20) };
            _periodRepository.Setup(x => x.GetAllAsync()).ReturnsAsync([_period, next]);

            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _sut.UpdateAsync(_period.Id, new UpdateMaintenancePeriodRequest { StartDate = _period.StartDate, EndDate = new DateOnly(2027, 3, 15) }));

            Assert.Contains("14-03-2027 t/m 20-03-2027", exception.Message);
        }

        // Ending a running period keeps its history while making the unit bookable today.
        [Fact]
        public async Task EndAsync_RunningPeriod_MovesPeriodToHistory()
        {
            _period.StartDate = Today.AddDays(-3);
            _period.EndDate = Today.AddDays(5);

            await _sut.EndAsync(_period.Id);

            Assert.Equal(Today.AddDays(-1), _period.EndDate);
            _periodRepository.Verify(x => x.UpdateAsync(_period), Times.Once);
            _periodRepository.Verify(x => x.DeleteAsync(It.IsAny<MaintenancePeriod>()), Times.Never);
        }

        [Fact]
        public async Task EndAsync_PeriodStartingToday_MovesPeriodToHistory()
        {
            _period.StartDate = Today;
            _period.EndDate = Today.AddDays(5);

            await _sut.EndAsync(_period.Id);

            Assert.Equal(Today.AddDays(-1), _period.StartDate);
            Assert.Equal(Today.AddDays(-1), _period.EndDate);
            _periodRepository.Verify(x => x.UpdateAsync(_period), Times.Once);
            _periodRepository.Verify(x => x.DeleteAsync(It.IsAny<MaintenancePeriod>()), Times.Never);
        }

        [Fact]
        public async Task EndAsync_PlannedPeriod_IsCancelled()
        {
            await _sut.EndAsync(_period.Id);

            _periodRepository.Verify(x => x.DeleteAsync(_period), Times.Once);
            _periodRepository.Verify(x => x.UpdateAsync(It.IsAny<MaintenancePeriod>()), Times.Never);
        }

        [Fact]
        public async Task EndAsync_EndedPeriod_ThrowsArgumentException()
        {
            _period.StartDate = Today.AddDays(-10);
            _period.EndDate = Today.AddDays(-2);

            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() => _sut.EndAsync(_period.Id));

            Assert.Equal("Deze periode is al afgelopen.", exception.Message);
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

        // A booking from 12 to 14 March: outside the stored period (1-10 March), inside an extension.
        private void SetupBookingInExtension()
            => _bookingRepository
                .Setup(x => x.GetByUnitIdInPeriodAsync(_unit.Id, It.IsAny<DateOnly>(), It.Is<DateOnly>(end => end > End)))
                .ReturnsAsync([new Booking { Id = Guid.NewGuid(), BookingNumber = "VP-2027-002", ArrivalDate = new DateOnly(2027, 3, 12), DepartureDate = new DateOnly(2027, 3, 14) }]);

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
