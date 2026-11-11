using Vakantiepark.Domain.Interfaces.Repositories;
using Vakantiepark.Application.Interfaces;
using Vakantiepark.Application.Services;
using Vakantiepark.Shared.DTOs.Booking;
using Vakantiepark.Domain.Entities;
using Xunit;
using Moq;

namespace Vakantiepark.UnitTests
{
    public class BookingServiceTests
    {
        private readonly Mock<IBookingRepository> _bookingRepository = new Mock<IBookingRepository>();
        private readonly Mock<IAccommodationUnitRepository> _accommodationUnitRepository = new Mock<IAccommodationUnitRepository>();
        private readonly Mock<IPriceService> _priceService = new Mock<IPriceService>();
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
                .Setup(x => x.AddAsync(It.IsAny<Booking>()))
                .ReturnsAsync((Booking booking) => booking);
            _priceService
                .Setup(x => x.CalculateAsync(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<int>()))
                .ReturnsAsync(TestData.CreatePriceSpecification());

            _sut = new BookingService(
                _bookingRepository.Object,
                _accommodationUnitRepository.Object,
                _priceService.Object);
        }

        [Fact]
        public async Task CreateAsync_ValidRequest_SavesBookingWithEnteredDetailsAndPrice()
        {
            CreateBookingRequest request = TestData.CreateNewBookingRequest();

            await _sut.CreateAsync(request);

            _bookingRepository.Verify(x => x.AddAsync(It.Is<Booking>(booking =>
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

            _bookingRepository.Verify(x => x.AddAsync(It.Is<Booking>(booking => booking.AccommodationUnitId == _secondUnitId)), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_AllUnitsBooked_ThrowsArgumentExceptionAndSavesNothing()
        {
            _bookingRepository
                .Setup(x => x.GetUnitIdsWithOverlappingBookingAsync(It.IsAny<List<Guid>>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
                .ReturnsAsync([_firstUnitId, _secondUnitId]);

            await Assert.ThrowsAsync<ArgumentException>(() => _sut.CreateAsync(TestData.CreateNewBookingRequest()));

            _bookingRepository.Verify(x => x.AddAsync(It.IsAny<Booking>()), Times.Never);
        }
    }
}
