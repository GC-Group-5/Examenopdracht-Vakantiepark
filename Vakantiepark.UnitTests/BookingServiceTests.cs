using Vakantiepark.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Vakantiepark.Domain.Interfaces.Helpers;
using Vakantiepark.Application.Interfaces;
using Vakantiepark.Application.Validations.Booking;
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
        private readonly Mock<IAccommodationTypeRepository> _accommodationTypeRepository = new Mock<IAccommodationTypeRepository>();
        private readonly Mock<IPriceService> _priceService = new Mock<IPriceService>();
        private readonly Mock<IBookingNumberGenerator> _bookingNumberGenerator = new Mock<IBookingNumberGenerator>();
        private readonly Mock<IEmailSender> _emailSender = new Mock<IEmailSender>();
        private readonly Mock<IRepository<Season>> _seasonRepository = new Mock<IRepository<Season>>();
        private readonly Mock<IRepository<ParkSettings>> _parkSettingsRepository = new Mock<IRepository<ParkSettings>>();
        private readonly Accommodation _firstUnit = TestData.CreateAccommodation("EL6-001", "Waterkant");
        private readonly Accommodation _secondUnit = TestData.CreateAccommodation("EL6-002", "Bosrand");
        private readonly BookingService _sut;

        public BookingServiceTests()
        {
            _accommodationTypeRepository
                .Setup(x => x.GetBySlugAsync("eik-luxe-6"))
                .ReturnsAsync(TestData.CreateAccommodationType(type => { type.Slug = "eik-luxe-6"; type.Name = "Eik Luxe 6"; }));
            _accommodationUnitRepository
                .Setup(x => x.GetBookableUnitsAsync(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<int>()))
                .ReturnsAsync(() => new List<Accommodation> { _firstUnit, _secondUnit });
            _bookingRepository
                .Setup(x => x.GetUnitIdsWithOverlappingBookingAsync(It.IsAny<List<Guid>>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
                .ReturnsAsync(new List<Guid>());
            _bookingRepository
                .Setup(x => x.TryAddAsync(It.IsAny<Booking>()))
                .ReturnsAsync(true);
            _priceService
                .Setup(x => x.CalculateAsync(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<int>()))
                .ReturnsAsync(TestData.CreatePriceSpecification());
            _bookingNumberGenerator
                .Setup(x => x.Generate())
                .Returns("K7M2XQ9P");
            _seasonRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<Season>());
            _parkSettingsRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<ParkSettings> { TestData.CreateParkSettings() });

            _sut = new BookingService(
                _bookingRepository.Object,
                _accommodationUnitRepository.Object,
                _accommodationTypeRepository.Object,
                _priceService.Object,
                _bookingNumberGenerator.Object,
                _seasonRepository.Object,
                _parkSettingsRepository.Object,
                new FixedTimeProvider(DateTimeOffset.UtcNow),
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
                && booking.TotalPrice == 817m
                && booking.Deposit == 163.40m)), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_ValidRequest_StoresEveryPersonWithBirthDate_MainBookerFirst()
        {
            CreateBookingRequest request = TestData.CreateNewBookingRequest();

            await _sut.CreateAsync(request);

            _bookingRepository.Verify(x => x.TryAddAsync(It.Is<Booking>(booking =>
                booking.Guests.Count == 4
                && booking.Guests[0].IsMainBooker
                && booking.Guests[0].Name == "Jan Jansen"
                && booking.Guests[0].BirthDate == new DateOnly(1985, 5, 1)
                && booking.Guests.Count(guest => guest.IsMainBooker) == 1
                && booking.Guests[3].Name == "Sara Jansen")), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_ValidRequest_TakesArrivalAndDepartureTimesFromParkSettings()
        {
            _parkSettingsRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<ParkSettings>
            {
                TestData.CreateParkSettings(settings => { settings.ArrivalTime = new TimeOnly(16, 0); settings.DepartureTime = new TimeOnly(9, 30); }),
            });

            BookingResponse response = await _sut.CreateAsync(TestData.CreateNewBookingRequest());

            Assert.Equal(new TimeOnly(16, 0), response.ArrivalTime);
            Assert.Equal(new TimeOnly(9, 30), response.DepartureTime);
        }

        [Fact]
        public async Task CreateAsync_FirstUnitBooked_AssignsFreeUnit()
        {
            _bookingRepository
                .Setup(x => x.GetUnitIdsWithOverlappingBookingAsync(It.IsAny<List<Guid>>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
                .ReturnsAsync(new List<Guid> { _firstUnit.Id });

            await _sut.CreateAsync(TestData.CreateNewBookingRequest());

            _bookingRepository.Verify(x => x.TryAddAsync(It.Is<Booking>(booking => booking.AccommodationUnitId == _secondUnit.Id)), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_PreferredZone_AssignsUnitInThatZone()
        {
            await _sut.CreateAsync(TestData.CreateNewBookingRequest(request => request.PreferredZone = "bosrand"));

            _bookingRepository.Verify(x => x.TryAddAsync(It.Is<Booking>(booking => booking.AccommodationUnitId == _secondUnit.Id)), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_PreferredZoneFull_ThrowsZoneMessageAndSavesNothing()
        {
            _bookingRepository
                .Setup(x => x.GetUnitIdsWithOverlappingBookingAsync(It.IsAny<List<Guid>>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
                .ReturnsAsync(new List<Guid> { _secondUnit.Id });

            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _sut.CreateAsync(TestData.CreateNewBookingRequest(request => request.PreferredZone = "Bosrand")));

            Assert.Contains("In de zone Bosrand is in deze periode geen Eik Luxe 6 meer vrij", exception.Message);
            _bookingRepository.Verify(x => x.TryAddAsync(It.IsAny<Booking>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_AllUnitsBooked_ThrowsArgumentExceptionAndSavesNothing()
        {
            _bookingRepository
                .Setup(x => x.GetUnitIdsWithOverlappingBookingAsync(It.IsAny<List<Guid>>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
                .ReturnsAsync(new List<Guid> { _firstUnit.Id, _secondUnit.Id });

            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() => _sut.CreateAsync(TestData.CreateNewBookingRequest()));

            Assert.Equal(CreateBookingRequestValidation.NotAvailableMessage, exception.Message);
            _bookingRepository.Verify(x => x.TryAddAsync(It.IsAny<Booking>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_NoUnitFitsTheParty_ThrowsArgumentException()
        {
            _accommodationUnitRepository
                .Setup(x => x.GetBookableUnitsAsync(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<int>()))
                .ReturnsAsync(new List<Accommodation>());

            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() => _sut.CreateAsync(TestData.CreateNewBookingRequest()));

            Assert.Contains("geen plek voor 4 personen", exception.Message);
        }

        [Fact]
        public async Task CreateAsync_UnknownAccommodationType_ThrowsArgumentException()
        {
            await Assert.ThrowsAsync<ArgumentException>(
                () => _sut.CreateAsync(TestData.CreateNewBookingRequest(request => request.AccommodationTypeSlug = "bestaat-niet")));

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

        // Two guests pass the availability check at the same moment; the database refuses the
        // second booking of the same unit, and the service then takes the next free unit.
        [Fact]
        public async Task CreateAsync_UnitTakenByConcurrentRequest_BooksNextFreeUnit()
        {
            _bookingRepository
                .Setup(x => x.TryAddAsync(It.Is<Booking>(booking => booking.AccommodationUnitId == _firstUnit.Id)))
                .ReturnsAsync(false);

            await _sut.CreateAsync(TestData.CreateNewBookingRequest());

            _bookingRepository.Verify(x => x.TryAddAsync(It.Is<Booking>(booking => booking.AccommodationUnitId == _secondUnit.Id)), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_EveryUnitTakenByConcurrentRequests_ThrowsNotAvailableMessage()
        {
            _bookingRepository
                .Setup(x => x.TryAddAsync(It.IsAny<Booking>()))
                .ReturnsAsync(false);

            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() => _sut.CreateAsync(TestData.CreateNewBookingRequest()));

            Assert.Equal(CreateBookingRequestValidation.NotAvailableMessage, exception.Message);
            _bookingRepository.Verify(x => x.TryAddAsync(It.IsAny<Booking>()), Times.Exactly(2));
        }

        [Fact]
        public async Task GetQuoteAsync_ValidStay_ReturnsNamePriceTimesAndCancellationTerms()
        {
            DateOnly arrival = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30);

            BookingQuoteResponse quote = await _sut.GetQuoteAsync("eik-luxe-6", arrival, arrival.AddDays(7), 4, "Waterkant");

            Assert.Equal("Eik Luxe 6", quote.AccommodationTypeName);
            Assert.Equal("Waterkant", quote.Zone);
            Assert.Equal(new TimeOnly(15, 0), quote.ArrivalTime);
            Assert.Equal(817m, quote.Price.TotalPrice);
            Assert.Equal(163.40m, quote.Price.Deposit);
            Assert.Equal(30, quote.FreeCancellationDaysBeforeArrival);
            Assert.Equal(7, quote.NoRefundDaysBeforeArrival);
            _bookingRepository.Verify(x => x.TryAddAsync(It.IsAny<Booking>()), Times.Never);
        }

        // US-09: confirmation email.

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
        public async Task CreateAsync_ValidRequest_EmailContainsStayPartyDepositAndCancellationRules()
        {
            await _sut.CreateAsync(TestData.CreateNewBookingRequest());

            _emailSender.Verify(x => x.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.Is<string>(html => html.Contains("K7M2XQ9P")
                    && html.Contains("Eik Luxe 6")
                    && html.Contains("Els Jansen")
                    && html.Contains("Aanbetaling (20%)")
                    && html.Contains("30 dagen")
                    && html.Contains("15:00")),
                It.Is<string>(text => text.Contains("K7M2XQ9P") && text.Contains("ANNULERINGSVOORWAARDEN"))), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_AllUnitsBooked_SendsNoEmail()
        {
            _bookingRepository
                .Setup(x => x.GetUnitIdsWithOverlappingBookingAsync(It.IsAny<List<Guid>>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
                .ReturnsAsync(new List<Guid> { _firstUnit.Id, _secondUnit.Id });

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
