using Microsoft.Extensions.Logging.Abstractions;
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
    // US-07 acceptance rule: when the stay period falls (partly) in a season
    // flagged Season.IsHighSeason, the booking must be at least
    // ParkSettings.HighSeasonMinimumNights long.
    public class HighSeasonMinimumNightsTests
    {
        private readonly Mock<IBookingRepository> _bookingRepository = new();
        private readonly Mock<IAccommodationUnitRepository> _accommodationUnitRepository = new();
        private readonly Mock<IPriceService> _priceService = new();
        private readonly Mock<IBookingNumberGenerator> _bookingNumberGenerator = new();
        private readonly Mock<IEmailSender> _emailSender = new();
        private readonly Mock<IRepository<Season>> _seasonRepository = new();
        private readonly Mock<IRepository<ParkSettings>> _parkSettingsRepository = new();
        private readonly Mock<IAccommodationTypeRepository> _accommodationTypeRepository = new();
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
            Season laag = TestData.CreateSeason(s =>
            {
                s.Name = "Laagseizoen 2026";
                s.StartDate = new DateOnly(2026, 1, 1);
                s.EndDate = new DateOnly(2026, 7, 3);
                s.IsHighSeason = false;
            });

            _seasonRepository.Setup(x => x.GetAllAsync()).ReturnsAsync([laag, zomer]);
            _parkSettingsRepository.Setup(x => x.GetAllAsync()).ReturnsAsync([new ParkSettings
            {
                Id = Guid.NewGuid(),
                ArrivalTime = new TimeOnly(15, 0),
                DepartureTime = new TimeOnly(10, 0),
                HighSeasonMinimumNights = 7,
                DepositPercentage = 20m,
            }]);
            _accommodationUnitRepository
                .Setup(x => x.GetInServiceIdsByTypeSlugAsync(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
                .ReturnsAsync([Guid.NewGuid()]);
            _bookingRepository
                .Setup(x => x.GetUnitIdsWithOverlappingBookingAsync(It.IsAny<List<Guid>>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
                .ReturnsAsync([]);
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
                _priceService.Object,
                _bookingNumberGenerator.Object,
                _seasonRepository.Object,
                _parkSettingsRepository.Object,
                _accommodationTypeRepository.Object,
                _emailSender.Object,
                NullLogger<BookingService>.Instance);
        }

        [Fact]
        public async Task CreateAsync_FiveNightsInHighSeason_IsRejectedWithDutchMessage()
        {
            CreateBookingRequest request = TestData.CreateNewBookingRequest(r =>
            {
                r.AccommodationTypeSlug = "berk-4";
                r.ArrivalDate = new DateOnly(2026, 7, 10);
                r.DepartureDate = new DateOnly(2026, 7, 15);
            });

            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() => _sut.CreateAsync(request));

            Assert.Contains("In het hoogseizoen (Zomervakantie 2026) moet een boeking minimaal 7 nachten zijn.", exception.Message);
            _bookingRepository.Verify(x => x.TryAddAsync(It.IsAny<Booking>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_StayStartingBeforeHighSeasonAndEndingInIt_IsRejected()
        {
            // "Valt de periode (deels) in een hoogseizoen": deels overlappen
            // telt ook, de minimumterm applies to the whole stay.
            CreateBookingRequest request = TestData.CreateNewBookingRequest(r =>
            {
                r.AccommodationTypeSlug = "berk-4";
                r.ArrivalDate = new DateOnly(2026, 8, 26);
                r.DepartureDate = new DateOnly(2026, 9, 1);
            });

            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() => _sut.CreateAsync(request));

            Assert.Contains("In het hoogseizoen (Zomervakantie 2026) moet een boeking minimaal 7 nachten zijn.", exception.Message);
            _bookingRepository.Verify(x => x.TryAddAsync(It.IsAny<Booking>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_OverlappingStayLongEnough_IsAllowed()
        {
            // Overlaps the summer end by two nights but meets the minimum: allowed.
            CreateBookingRequest request = TestData.CreateNewBookingRequest(r =>
            {
                r.AccommodationTypeSlug = "berk-4";
                r.ArrivalDate = new DateOnly(2026, 8, 26);
                r.DepartureDate = new DateOnly(2026, 9, 6);
            });

            await _sut.CreateAsync(request);

            _bookingRepository.Verify(x => x.TryAddAsync(It.IsAny<Booking>()), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_StayEndingJustBeforeHighSeason_IsAllowed()
        {
            // Nights end 3 juli: no night falls in the high season, so no minimum.
            CreateBookingRequest request = TestData.CreateNewBookingRequest(r =>
            {
                r.AccommodationTypeSlug = "berk-4";
                r.ArrivalDate = new DateOnly(2026, 6, 28);
                r.DepartureDate = new DateOnly(2026, 7, 4);
            });

            await _sut.CreateAsync(request);

            _bookingRepository.Verify(x => x.TryAddAsync(It.IsAny<Booking>()), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_SevenNightsInHighSeason_IsAllowed()
        {
            CreateBookingRequest request = TestData.CreateNewBookingRequest(r =>
            {
                r.AccommodationTypeSlug = "berk-4";
                r.ArrivalDate = new DateOnly(2026, 7, 10);
                r.DepartureDate = new DateOnly(2026, 7, 17);
            });

            await _sut.CreateAsync(request);

            _bookingRepository.Verify(x => x.TryAddAsync(It.IsAny<Booking>()), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_FiveNightsOutsideHighSeason_IsAllowed()
        {
            CreateBookingRequest request = TestData.CreateNewBookingRequest(r =>
            {
                r.AccommodationTypeSlug = "berk-4";
                r.ArrivalDate = new DateOnly(2026, 3, 10);
                r.DepartureDate = new DateOnly(2026, 3, 15);
            });

            await _sut.CreateAsync(request);

            _bookingRepository.Verify(x => x.TryAddAsync(It.IsAny<Booking>()), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_NoSeasonMarkedAsHigh_IsAllowed()
        {
            _seasonRepository.Setup(x => x.GetAllAsync()).ReturnsAsync([
                TestData.CreateSeason(s =>
                {
                    s.Name = "Zonder hoogseizoen";
                    s.StartDate = new DateOnly(2026, 1, 1);
                    s.EndDate = new DateOnly(2026, 12, 31);
                    s.IsHighSeason = false;
                }),
            ]);

            CreateBookingRequest request = TestData.CreateNewBookingRequest(r =>
            {
                r.AccommodationTypeSlug = "berk-4";
                r.ArrivalDate = new DateOnly(2026, 7, 10);
                r.DepartureDate = new DateOnly(2026, 7, 15);
            });

            await _sut.CreateAsync(request);

            _bookingRepository.Verify(x => x.TryAddAsync(It.IsAny<Booking>()), Times.Once);
        }
    }
}
