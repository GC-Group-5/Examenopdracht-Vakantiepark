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
    // US-07 acceptance rule: a booking that starts in the high season
    // (Season.IsHighSeason) must be at least HighSeasonMinimumNights long.
    public class HoogseizoenMinimumNachtenTests
    {
        private readonly Mock<IBookingRepository> _bookingRepository = new();
        private readonly Mock<IAccommodationUnitRepository> _accommodationUnitRepository = new();
        private readonly Mock<IPriceService> _priceService = new();
        private readonly Mock<IBookingNumberGenerator> _bookingNumberGenerator = new();
        private readonly Mock<IRepository<Season>> _seasonRepository = new();
        private readonly Mock<IRepository<ParkSettings>> _parkSettingsRepository = new();
        private readonly BookingService _sut;

        public HoogseizoenMinimumNachtenTests()
        {
            Season hoog = TestData.CreateSeason(s =>
            {
                s.Name = "Hoogseizoen";
                s.StartDate = new DateOnly(2026, 7, 1);
                s.EndDate = new DateOnly(2026, 8, 31);
                s.IsHighSeason = true;
            });
            Season laag = TestData.CreateSeason(s =>
            {
                s.Name = "Laagseizoen";
                s.StartDate = new DateOnly(2026, 1, 1);
                s.EndDate = new DateOnly(2026, 6, 30);
                s.IsHighSeason = false;
            });

            _seasonRepository.Setup(x => x.GetAllAsync()).ReturnsAsync([hoog, laag]);
            _parkSettingsRepository.Setup(x => x.GetAllAsync()).ReturnsAsync([new ParkSettings
            {
                Id = Guid.NewGuid(),
                ArrivalTime = new TimeOnly(15, 0),
                DepartureTime = new TimeOnly(10, 0),
                HighSeasonMinimumNights = 7,
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
                _parkSettingsRepository.Object);
        }

        [Fact]
        public async Task CreateAsync_VijfNachtenInHoogseizoen_WordtAfgewezenMetNederlandseMelding()
        {
            CreateBookingRequest request = TestData.CreateNewBookingRequest(r =>
            {
                r.AccommodationTypeSlug = "berk-4";
                r.ArrivalDate = new DateOnly(2026, 7, 10);
                r.DepartureDate = new DateOnly(2026, 7, 15);
            });

            Exception exception = await Assert.ThrowsAnyAsync<Exception>(() => _sut.CreateAsync(request));

            Assert.Contains("In het hoogseizoen (Hoogseizoen) moet een boeking minimaal 7 nachten zijn.", exception.Message);
            _bookingRepository.Verify(x => x.TryAddAsync(It.IsAny<Booking>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_ZevenNachtenInHoogseizoen_WordtTolaten()
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
        public async Task CreateAsync_VijfNachtenBuitenHoogseizoen_WordtTolaten()
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
        public async Task CreateAsync_StartInLaagseizoenEindInHoogseizoen_WordtTolaten()
        {
            // The minimum-nights rule looks at the season of the arrival date;
            // the mixed price for the nights is the price service's job.
            CreateBookingRequest request = TestData.CreateNewBookingRequest(r =>
            {
                r.AccommodationTypeSlug = "berk-4";
                r.ArrivalDate = new DateOnly(2026, 6, 29);
                r.DepartureDate = new DateOnly(2026, 7, 3);
            });

            await _sut.CreateAsync(request);

            _bookingRepository.Verify(x => x.TryAddAsync(It.IsAny<Booking>()), Times.Once);
        }
    }
}
