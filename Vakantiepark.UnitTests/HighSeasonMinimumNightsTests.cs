using Moq;
using Vakantiepark.Application.Interfaces;
using Vakantiepark.Application.Services;
using Vakantiepark.Domain.Entities;
using Vakantiepark.Domain.Interfaces.Helpers;
using Vakantiepark.Domain.Interfaces.Repositories;
using Vakantiepark.Shared.DTOs.Booking;
using Xunit;

namespace Vakantiepark.UnitTests
{
    // When the stay period falls (partly) in a season flagged Season.IsHighSeason, the booking
    // must be at least ParkSettings.HighSeasonMinimumNights long. "Today" is fixed at the start
    // of 2026, so the summer 2026 dates in these tests lie in the future.
    public class HighSeasonMinimumNightsTests
    {
        private readonly Mock<IBookingRepository> _bookingRepository = new();
        private readonly Mock<IAccommodationUnitRepository> _accommodationUnitRepository = new();
        private readonly Mock<IAccommodationTypeRepository> _accommodationTypeRepository = new();
        private readonly Mock<IPriceService> _priceService = new();
        private readonly Mock<IBookingNumberGenerator> _bookingNumberGenerator = new();
        private readonly Mock<IRepository<Season>> _seasonRepository = new();
        private readonly Mock<IRepository<ParkSettings>> _parkSettingsRepository = new();
        private readonly BookingService _sut;

        public HighSeasonMinimumNightsTests()
        {
            Season zomer = TestData.CreateSeason(s =>
            {
                s.Name = "Zomervakantie 2026";
                s.StartDate = new DateOnly(2026, 7, 4);
                s.EndDate = new DateOnly(2026, 8, 30);
                s.IsHighSeason = true;
            });

            _seasonRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<Season> { zomer });
            _parkSettingsRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<ParkSettings> { TestData.CreateParkSettings() });
            _accommodationTypeRepository.Setup(x => x.GetBySlugAsync("berk-4")).ReturnsAsync(TestData.CreateAccommodationType());
            _accommodationUnitRepository
                .Setup(x => x.GetBookableUnitsAsync(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<int>()))
                .ReturnsAsync(() => new List<Accommodation> { TestData.CreateAccommodation("BK4-001") });
            _bookingRepository
                .Setup(x => x.GetUnitIdsWithOverlappingBookingAsync(It.IsAny<List<Guid>>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
                .ReturnsAsync(new List<Guid>());
            _bookingRepository
                .Setup(x => x.TryAddAsync(It.IsAny<Booking>()))
                .ReturnsAsync(true);
            _bookingNumberGenerator
                .Setup(x => x.Generate())
                .Returns("K7M2XQ9P");
            _priceService
                .Setup(x => x.CalculateAsync(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<int>()))
                .ReturnsAsync(TestData.CreatePriceSpecification());

            _sut = new BookingService(
                _bookingRepository.Object,
                _accommodationUnitRepository.Object,
                _accommodationTypeRepository.Object,
                _priceService.Object,
                _bookingNumberGenerator.Object,
                _seasonRepository.Object,
                _parkSettingsRepository.Object,
                new FixedTimeProvider(new DateTimeOffset(2026, 1, 15, 9, 0, 0, TimeSpan.Zero)));
        }

        private static CreateBookingRequest Stay(int arrivalMonth, int arrivalDay, int departureMonth, int departureDay)
            => TestData.CreateNewBookingRequest(r =>
            {
                r.AccommodationTypeSlug = "berk-4";
                r.ArrivalDate = new DateOnly(2026, arrivalMonth, arrivalDay);
                r.DepartureDate = new DateOnly(2026, departureMonth, departureDay);
            });

        [Fact]
        public async Task CreateAsync_FiveNightsInHighSeason_IsRejectedWithDutchMessage()
        {
            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() => _sut.CreateAsync(Stay(7, 10, 7, 15)));

            Assert.Contains("In het hoogseizoen (Zomervakantie 2026) moet een boeking minimaal 7 nachten zijn.", exception.Message);
            _bookingRepository.Verify(x => x.TryAddAsync(It.IsAny<Booking>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_StayStartingInHighSeasonAndEndingAfterIt_IsRejected()
        {
            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() => _sut.CreateAsync(Stay(8, 26, 9, 1)));

            Assert.Contains("minimaal 7 nachten", exception.Message);
            _bookingRepository.Verify(x => x.TryAddAsync(It.IsAny<Booking>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_OverlappingStayLongEnough_IsAllowed()
        {
            await _sut.CreateAsync(Stay(8, 26, 9, 6));

            _bookingRepository.Verify(x => x.TryAddAsync(It.IsAny<Booking>()), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_StayEndingJustBeforeHighSeason_IsAllowed()
        {
            // The last night is 3 July; the departure day itself is not a night of the stay.
            await _sut.CreateAsync(Stay(6, 28, 7, 4));

            _bookingRepository.Verify(x => x.TryAddAsync(It.IsAny<Booking>()), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_SevenNightsInHighSeason_IsAllowed()
        {
            await _sut.CreateAsync(Stay(7, 10, 7, 17));

            _bookingRepository.Verify(x => x.TryAddAsync(It.IsAny<Booking>()), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_FiveNightsOutsideHighSeason_IsAllowed()
        {
            await _sut.CreateAsync(Stay(3, 10, 3, 15));

            _bookingRepository.Verify(x => x.TryAddAsync(It.IsAny<Booking>()), Times.Once);
        }

        [Fact]
        public async Task GetQuoteAsync_FiveNightsInHighSeason_IsRejectedBeforeTheGuestFillsInDetails()
        {
            await Assert.ThrowsAsync<ArgumentException>(
                () => _sut.GetQuoteAsync("berk-4", new DateOnly(2026, 7, 10), new DateOnly(2026, 7, 15), 4, null));
        }
    }
}
