using Vakantiepark.Domain.Interfaces.Repositories;
using Vakantiepark.Domain.Interfaces.Helpers;
using Vakantiepark.Application.Interfaces;
using Vakantiepark.Application.Validations.Booking;
using Vakantiepark.Application.Services;
using Vakantiepark.Shared.DTOs.Booking;
using Vakantiepark.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Moq;

namespace Vakantiepark.UnitTests
{
    public class BookingServiceTests
    {
        private readonly Mock<IBookingRepository> _bookingRepository = new Mock<IBookingRepository>();
        private readonly Mock<IAccommodationUnitRepository> _accommodationUnitRepository = new Mock<IAccommodationUnitRepository>();
        private readonly Mock<IParkSettingsRepository> _parkSettingsRepository = new Mock<IParkSettingsRepository>();
        private readonly Mock<IPriceService> _priceService = new Mock<IPriceService>();
        private readonly Mock<IBookingNumberGenerator> _bookingNumberGenerator = new Mock<IBookingNumberGenerator>();
        private readonly Mock<IEmailSender> _emailSender = new Mock<IEmailSender>();
        private readonly Guid _firstUnitId = Guid.NewGuid();
        private readonly Guid _secondUnitId = Guid.NewGuid();
        private readonly BookingService _sut;

        public BookingServiceTests()
        {
            _accommodationUnitRepository
                .Setup(x => x.GetInServiceIdsByTypeSlugAsync(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
                .ReturnsAsync([_firstUnitId, _secondUnitId]);
            _bookingRepository
                .Setup(x => x.GetUnitIdsWithOverlappingBookingAsync(It.IsAny<List<Guid>>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
                .ReturnsAsync([]);
            _bookingRepository
                .Setup(x => x.TryAddAsync(It.IsAny<Booking>()))
                .ReturnsAsync(true);
            _priceService
                .Setup(x => x.CalculateAsync(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<int>()))
                .ReturnsAsync(TestData.CreatePriceSpecification());
            _parkSettingsRepository
                .Setup(x => x.GetAsync())
                .ReturnsAsync(TestData.CreateParkSettings());
            _bookingNumberGenerator
                .Setup(x => x.Generate())
                .Returns("K7M2XQ9P");

            _sut = new BookingService(
                _bookingRepository.Object,
                _accommodationUnitRepository.Object,
                _parkSettingsRepository.Object,
                _priceService.Object,
                _bookingNumberGenerator.Object,
                _emailSender.Object,
                NullLogger<BookingService>.Instance);
        }

        [Fact]
        public async Task CreateAsync_ValidRequest_SavesBookingWithEnteredDetailsAndPrice()
        {
            CreateBookingRequest request = TestData.CreateNewBookingRequest();

            await _sut.CreateAsync(request);

            _bookingRepository.Verify(x => x.TryAddAsync(It.Is<Booking>(booking =>
                booking.Name == request.Name
                && booking.Email == request.Email
                && booking.PhoneNumber == request.PhoneNumber
                && booking.ArrivalDate == request.ArrivalDate
                && booking.DepartureDate == request.DepartureDate
                && booking.NumberOfGuests == request.NumberOfGuests
                && booking.TotalPrice == 817m)), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_FirstUnitBooked_AssignsFreeUnit()
        {
            _bookingRepository
                .Setup(x => x.GetUnitIdsWithOverlappingBookingAsync(It.IsAny<List<Guid>>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
                .ReturnsAsync([_firstUnitId]);

            await _sut.CreateAsync(TestData.CreateNewBookingRequest());

            _bookingRepository.Verify(x => x.TryAddAsync(It.Is<Booking>(booking => booking.AccommodationUnitId == _secondUnitId)), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_AllUnitsBooked_ThrowsArgumentExceptionAndSavesNothing()
        {
            _bookingRepository
                .Setup(x => x.GetUnitIdsWithOverlappingBookingAsync(It.IsAny<List<Guid>>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
                .ReturnsAsync([_firstUnitId, _secondUnitId]);

            await Assert.ThrowsAsync<ArgumentException>(() => _sut.CreateAsync(TestData.CreateNewBookingRequest()));

            _bookingRepository.Verify(x => x.TryAddAsync(It.IsAny<Booking>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_ValidRequest_ReturnsGeneratedBookingNumber()
        {
            BookingResponse response = await _sut.CreateAsync(TestData.CreateNewBookingRequest());

            Assert.Equal("K7M2XQ9P", response.BookingNumber);
            _bookingRepository.Verify(x => x.TryAddAsync(It.Is<Booking>(booking => booking.BookingNumber == "K7M2XQ9P")), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_BookingNumberAlreadyExists_UsesNewNumber()
        {
            _bookingNumberGenerator
                .SetupSequence(x => x.Generate())
                .Returns("AAAA2222")
                .Returns("BBBB3333");
            _bookingRepository
                .Setup(x => x.ExistsByBookingNumberIncludingDeletedAsync("AAAA2222"))
                .ReturnsAsync(true);

            BookingResponse response = await _sut.CreateAsync(TestData.CreateNewBookingRequest());

            Assert.Equal("BBBB3333", response.BookingNumber);
        }

        [Fact]
        public async Task CreateAsync_NoUniqueBookingNumberFound_ThrowsInvalidOperationExceptionAndSavesNothing()
        {
            _bookingRepository
                .Setup(x => x.ExistsByBookingNumberIncludingDeletedAsync(It.IsAny<string>()))
                .ReturnsAsync(true);

            await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateAsync(TestData.CreateNewBookingRequest()));

            _bookingRepository.Verify(x => x.TryAddAsync(It.IsAny<Booking>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_UnitBookedByConcurrentRequest_ThrowsArgumentException()
        {
            _bookingRepository
                .Setup(x => x.TryAddAsync(It.IsAny<Booking>()))
                .ReturnsAsync(false);

            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() => _sut.CreateAsync(TestData.CreateNewBookingRequest()));

            Assert.Equal(CreateBookingRequestValidation.NotAvailableMessage, exception.Message);
        }

        [Fact]
        public async Task CreateAsync_ValidRequest_SavesBirthDateTravelersAndParkTimes()
        {
            CreateBookingRequest request = TestData.CreateNewBookingRequest();

            await _sut.CreateAsync(request);

            _bookingRepository.Verify(x => x.TryAddAsync(It.Is<Booking>(booking =>
                booking.BirthDate == request.BirthDate
                && booking.Travelers.Count == 3
                && booking.Travelers[0].Name == "Petra Jansen"
                && booking.Travelers[0].BirthDate == new DateOnly(1987, 6, 2)
                && booking.ArrivalTime == new TimeOnly(15, 0)
                && booking.DepartureTime == new TimeOnly(10, 0))), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_MainBookerUnder18OnArrival_ThrowsArgumentExceptionAndSavesNothing()
        {
            CreateBookingRequest request = TestData.CreateNewBookingRequest(r =>
                r.BirthDate = r.ArrivalDate.AddYears(-18).AddDays(1));

            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() => _sut.CreateAsync(request));

            Assert.Equal(CreateBookingRequestValidation.MainBookerTooYoungMessage, exception.Message);
            _bookingRepository.Verify(x => x.TryAddAsync(It.IsAny<Booking>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_MainBookerTurns18OnArrival_SavesBooking()
        {
            CreateBookingRequest request = TestData.CreateNewBookingRequest(r =>
                r.BirthDate = r.ArrivalDate.AddYears(-18));

            await _sut.CreateAsync(request);

            _bookingRepository.Verify(x => x.TryAddAsync(It.IsAny<Booking>()), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_TravelerCountDoesNotMatchGuests_ThrowsArgumentExceptionAndSavesNothing()
        {
            CreateBookingRequest request = TestData.CreateNewBookingRequest(r => r.Travelers.RemoveAt(0));

            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() => _sut.CreateAsync(request));

            Assert.Equal(CreateBookingRequestValidation.TravelerCountMessage, exception.Message);
            _bookingRepository.Verify(x => x.TryAddAsync(It.IsAny<Booking>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_TravelerWithoutBirthDate_ThrowsArgumentExceptionAndSavesNothing()
        {
            CreateBookingRequest request = TestData.CreateNewBookingRequest(r => r.Travelers[1].BirthDate = null);

            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() => _sut.CreateAsync(request));

            Assert.Equal(CreateBookingRequestValidation.TravelerDetailsMessage, exception.Message);
            _bookingRepository.Verify(x => x.TryAddAsync(It.IsAny<Booking>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_PreferredZone_AssignsFreeUnitInThatZone()
        {
            _accommodationUnitRepository
                .Setup(x => x.GetIdsInZoneAsync(It.IsAny<List<Guid>>(), "Waterkant"))
                .ReturnsAsync([_secondUnitId]);

            await _sut.CreateAsync(TestData.CreateNewBookingRequest(r => r.PreferredZone = "Waterkant"));

            _bookingRepository.Verify(x => x.TryAddAsync(It.Is<Booking>(booking => booking.AccommodationUnitId == _secondUnitId)), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_NoFreeUnitInPreferredZone_ThrowsArgumentExceptionAndSavesNothing()
        {
            _accommodationUnitRepository
                .Setup(x => x.GetIdsInZoneAsync(It.IsAny<List<Guid>>(), "Waterkant"))
                .ReturnsAsync([]);

            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                _sut.CreateAsync(TestData.CreateNewBookingRequest(r => r.PreferredZone = "Waterkant")));

            Assert.Equal(CreateBookingRequestValidation.NotAvailableInZoneMessage, exception.Message);
            _bookingRepository.Verify(x => x.TryAddAsync(It.IsAny<Booking>()), Times.Never);
        }

        [Fact]
        public async Task GetInfoAsync_KnownType_ReturnsTypeZonesAndParkRules()
        {
            _accommodationUnitRepository
                .Setup(x => x.GetTypeNameBySlugAsync("berk-4"))
                .ReturnsAsync("Berk 4");
            _accommodationUnitRepository
                .Setup(x => x.GetZoneNamesByTypeSlugAsync("berk-4"))
                .ReturnsAsync(["Bosrand", "Waterkant"]);

            BookingInfoResponse info = await _sut.GetInfoAsync("berk-4");

            Assert.Equal("Berk 4", info.AccommodationTypeName);
            Assert.Equal(new List<string> { "Bosrand", "Waterkant" }, info.Zones);
            Assert.Equal(new TimeOnly(15, 0), info.ArrivalTime);
            Assert.Equal(30m, info.DepositPercentage);
            Assert.Equal(42, info.FreeCancellationDaysBeforeArrival);
        }

        [Fact]
        public async Task GetInfoAsync_UnknownType_ThrowsKeyNotFoundException()
        {
            _accommodationUnitRepository
                .Setup(x => x.GetTypeNameBySlugAsync(It.IsAny<string>()))
                .ReturnsAsync((string?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.GetInfoAsync("bestaat-niet"));
        }
    }
}
