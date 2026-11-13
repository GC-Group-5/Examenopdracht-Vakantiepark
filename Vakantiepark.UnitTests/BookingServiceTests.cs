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
        private readonly Mock<IPriceService> _priceService = new Mock<IPriceService>();
        private readonly Mock<IBookingNumberGenerator> _bookingNumberGenerator = new Mock<IBookingNumberGenerator>();
        private readonly Mock<IRepository<Season>> _seasonRepository = new();
        private readonly Mock<IRepository<ParkSettings>> _parkSettingsRepository = new();
        private readonly Mock<IAccommodationTypeRepository> _accommodationTypeRepository = new();
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
            _bookingNumberGenerator
                .Setup(x => x.Generate())
                .Returns("K7M2XQ9P");
            _seasonRepository.Setup(x => x.GetAllAsync()).ReturnsAsync([]);
            // The mail needs the deposit percentage and cancellation rules.
            _parkSettingsRepository.Setup(x => x.GetAllAsync()).ReturnsAsync([new ParkSettings
            {
                DepositPercentage = 30m,
                FreeCancellationDaysBeforeArrival = 42,
                NoRefundDaysBeforeArrival = 14
            }]);
            _accommodationTypeRepository
                .Setup(x => x.GetBySlugAsync(It.IsAny<string>()))
                .ReturnsAsync(TestData.CreateAccommodationType(t => t.Name = "Eik Luxe 6"));

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
        public async Task CreateAsync_ValidRequest_SendsOneConfirmationEmailToGuest()
        {
            CreateBookingRequest request = TestData.CreateNewBookingRequest();

            await _sut.CreateAsync(request);

            _emailSender.Verify(x => x.SendAsync(
                request.Email,
                It.Is<string>(subject => subject.Contains("K7M2XQ9P")),
                It.IsAny<string>(),
                It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_ValidRequest_EmailContainsStayDepositAndCancellationRules()
        {
            await _sut.CreateAsync(TestData.CreateNewBookingRequest());

            _emailSender.Verify(x => x.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.Is<string>(html => html.Contains("K7M2XQ9P") && html.Contains("Eik Luxe 6") && html.Contains("Aanbetaling (30%)") && html.Contains("42 dagen")),
                It.Is<string>(text => text.Contains("K7M2XQ9P") && text.Contains("ANNULERINGSVOORWAARDEN"))), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_AllUnitsBooked_SendsNoEmail()
        {
            _bookingRepository
                .Setup(x => x.GetUnitIdsWithOverlappingBookingAsync(It.IsAny<List<Guid>>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
                .ReturnsAsync([_firstUnitId, _secondUnitId]);

            await Assert.ThrowsAsync<ArgumentException>(() => _sut.CreateAsync(TestData.CreateNewBookingRequest()));

            _emailSender.Verify(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_EmailFails_KeepsBookingAndReturnsBookingNumber()
        {
            _emailSender
                .Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("Mail server down"));

            BookingResponse response = await _sut.CreateAsync(TestData.CreateNewBookingRequest());

            Assert.Equal("K7M2XQ9P", response.BookingNumber);
            _bookingRepository.Verify(x => x.TryAddAsync(It.IsAny<Booking>()), Times.Once);
        }
    }
}
