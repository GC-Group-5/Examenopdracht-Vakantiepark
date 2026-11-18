using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Vakantiepark.Application.Interfaces;
using Vakantiepark.Application.Services;
using Vakantiepark.Application.Settings;
using Vakantiepark.Domain.Entities;
using Vakantiepark.Domain.Interfaces.Helpers;
using Vakantiepark.Domain.Interfaces.Repositories;
using Vakantiepark.Shared.DTOs.Booking;
using Xunit;

namespace Vakantiepark.UnitTests
{
    // US-12: cancelling and changing the guest's own booking.
    public class ChangeAndCancelBookingTests
    {
        private const string Token = "a3f1c2d4e5b6a7980112233445566778899aabbccddeeff00112233445566778";
        private static readonly DateOnly Today = new DateOnly(2027, 3, 1);

        private readonly Mock<IBookingRepository> _bookingRepository = new Mock<IBookingRepository>();
        private readonly Mock<IAccommodationUnitRepository> _unitRepository = new Mock<IAccommodationUnitRepository>();
        private readonly Mock<IPriceService> _priceService = new Mock<IPriceService>();
        private readonly Mock<IRepository<Season>> _seasonRepository = new Mock<IRepository<Season>>();
        private readonly Mock<IEmailSender> _emailSender = new Mock<IEmailSender>();
        private readonly AccommodationType _type = TestData.CreateAccommodationType(type => { type.Slug = "eik-luxe-6"; type.Name = "Eik Luxe 6"; });
        private readonly Accommodation _currentUnit;
        private readonly Accommodation _otherUnit;
        private readonly Booking _booking;
        private readonly BookingService _sut;

        public ChangeAndCancelBookingTests()
        {
            _currentUnit = TestData.CreateAccommodation("EL6-001", configure: unit => unit.Type = _type);
            _otherUnit = TestData.CreateAccommodation("EL6-002", configure: unit => unit.Type = _type);
            _booking = CreateBooking(daysBeforeArrival: 60);

            _bookingRepository.Setup(x => x.GetByAccessTokenAsync(Token)).ReturnsAsync(() => _booking);
            _bookingRepository
                .Setup(x => x.GetUnitIdsWithOverlappingBookingExcludingAsync(It.IsAny<List<Guid>>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<Guid>()))
                .ReturnsAsync(new List<Guid>());
            _bookingRepository.Setup(x => x.TryUpdateAsync(It.IsAny<Booking>(), It.IsAny<List<BookingGuest>>())).ReturnsAsync(true);
            _unitRepository
                .Setup(x => x.GetInServiceUnitsAsync(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
                .ReturnsAsync(() => new List<Accommodation> { _currentUnit, _otherUnit });
            _priceService
                .Setup(x => x.CalculateAsync(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<int>()))
                .ReturnsAsync(TestData.CreatePriceSpecification());
            _seasonRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<Season>());

            Mock<IRepository<ParkSettings>> parkSettingsRepository = new Mock<IRepository<ParkSettings>>();
            parkSettingsRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<ParkSettings> { TestData.CreateParkSettings() });

            _sut = new BookingService(
                _bookingRepository.Object,
                _unitRepository.Object,
                new Mock<IAccommodationTypeRepository>().Object,
                _priceService.Object,
                new Mock<IBookingNumberGenerator>().Object,
                _seasonRepository.Object,
                parkSettingsRepository.Object,
                new FixedTimeProvider(new DateTimeOffset(Today.ToDateTime(new TimeOnly(9, 0)), TimeSpan.Zero)),
                _emailSender.Object,
                new FrontendSettings { BaseUrl = "https://localhost:4200" },
                NullLogger<BookingService>.Instance);
        }

        // ---------- Cancelling ----------

        [Theory]
        [InlineData(60, 0)]
        [InlineData(20, 120)]
        [InlineData(3, 590)]
        public async Task GetCancellationQuote_ShowsWhatTheGuestLoses(int daysBeforeArrival, decimal expectedFee)
        {
            SetArrival(daysBeforeArrival);

            CancellationQuoteResponse quote = await _sut.GetCancellationQuoteAsync(Access(), null);

            Assert.Equal(expectedFee, quote.CancellationFee);
            Assert.False(string.IsNullOrWhiteSpace(quote.Explanation));
            _bookingRepository.Verify(x => x.CancelAsync(It.IsAny<Booking>(), It.IsAny<decimal>()), Times.Never);
        }

        [Fact]
        public async Task Cancel_SavesTheFeeWithTheBookingAndSendsAnEmail()
        {
            SetArrival(20);

            await _sut.CancelAsync(Access(), null);

            _bookingRepository.Verify(x => x.CancelAsync(_booking, 120m), Times.Once);
            _emailSender.Verify(x => x.SendAsync(
                "jan@example.nl",
                It.Is<string>(subject => subject.Contains("Annulering")),
                It.Is<string>(html => html.Contains("120")),
                It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task Cancel_OnTheArrivalDay_IsRefusedAndNothingIsSaved()
        {
            SetArrival(0);

            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() => _sut.CancelAsync(Access(), null));

            Assert.Equal(BookingService.CannotCancelMessage, exception.Message);
            _bookingRepository.Verify(x => x.CancelAsync(It.IsAny<Booking>(), It.IsAny<decimal>()), Times.Never);
        }

        [Fact]
        public async Task Cancel_WithSomeoneElsesCode_FindsNothing()
        {
            BookingAccessRequest access = new BookingAccessRequest() { Token = "verkeerde-code" };

            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() => _sut.CancelAsync(access, null));

            Assert.Equal(BookingService.InvalidLinkMessage, exception.Message);
            _bookingRepository.Verify(x => x.CancelAsync(It.IsAny<Booking>(), It.IsAny<decimal>()), Times.Never);
        }

        [Fact]
        public async Task Cancel_GuestLoggedInWithBookingNumberAndEmail_UsesTheBookingFromHisLogin()
        {
            _bookingRepository.Setup(x => x.GetByIdWithDetailsAsync(_booking.Id)).ReturnsAsync(() => _booking);
            SetArrival(20);

            await _sut.CancelAsync(new BookingAccessRequest(), _booking.Id);

            _bookingRepository.Verify(x => x.CancelAsync(_booking, 120m), Times.Once);
        }

        [Fact]
        public async Task Cancel_WithoutCodeAndWithoutGuestLogin_FindsNothing()
        {
            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() => _sut.CancelAsync(new BookingAccessRequest(), null));

            Assert.Equal(BookingService.NotFoundMessage, exception.Message);
        }

        // ---------- Changing ----------

        [Fact]
        public async Task GetChangeQuote_ShowsTheOldAndTheNewPrice_WithoutSaving()
        {
            ChangeQuoteResponse quote = await _sut.GetChangeQuoteAsync(ChangeRequest(shiftDays: 2), null);

            Assert.Equal(600m, quote.OldTotalPrice);
            Assert.Equal(817m, quote.NewPrice.TotalPrice);
            Assert.False(quote.OtherUnit);
            _bookingRepository.Verify(x => x.TryUpdateAsync(It.IsAny<Booking>(), It.IsAny<List<BookingGuest>>()), Times.Never);
        }

        [Fact]
        public async Task Change_LeavesTheOwnBookingOutOfTheAvailabilityCheck()
        {
            await _sut.GetChangeQuoteAsync(ChangeRequest(shiftDays: 2), null);

            _bookingRepository.Verify(x => x.GetUnitIdsWithOverlappingBookingExcludingAsync(
                It.IsAny<List<Guid>>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), _booking.Id), Times.Once);
        }

        [Fact]
        public async Task Change_CurrentUnitTaken_GetsAnotherFreeUnitOfTheSameType()
        {
            UnitsTaken(_currentUnit.Id);

            MyBookingResponse response = await _sut.ChangeAsync(ChangeRequest(shiftDays: 2), null);

            Assert.Equal(_otherUnit.Id, _booking.AccommodationUnitId);
            Assert.Equal("Eik Luxe 6", response.AccommodationTypeName);
        }

        [Fact]
        public async Task Change_NoFreeUnit_GivesAMessageAndKeepsTheOldBooking()
        {
            UnitsTaken(_currentUnit.Id, _otherUnit.Id);
            DateOnly oldArrival = _booking.ArrivalDate;

            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() => _sut.ChangeAsync(ChangeRequest(shiftDays: 2), null));

            Assert.Equal(BookingService.NoUnitForChangeMessage, exception.Message);
            Assert.Equal(oldArrival, _booking.ArrivalDate);
            _bookingRepository.Verify(x => x.TryUpdateAsync(It.IsAny<Booking>(), It.IsAny<List<BookingGuest>>()), Times.Never);
        }

        [Fact]
        public async Task Change_SomeoneElseBookedTheUnitMeanwhile_GivesAMessage()
        {
            _bookingRepository.Setup(x => x.TryUpdateAsync(It.IsAny<Booking>(), It.IsAny<List<BookingGuest>>())).ReturnsAsync(false);

            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() => _sut.ChangeAsync(ChangeRequest(shiftDays: 2), null));

            Assert.Equal(BookingService.NoUnitForChangeMessage, exception.Message);
            _emailSender.Verify(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Change_SavesTheNewDatesAndPriceAndSendsAnEmail()
        {
            ChangeBookingRequest request = ChangeRequest(shiftDays: 2);

            await _sut.ChangeAsync(request, null);

            Assert.Equal(request.ArrivalDate, _booking.ArrivalDate);
            Assert.Equal(817m, _booking.TotalPrice);
            Assert.Equal(163.40m, _booking.Deposit);
            _emailSender.Verify(x => x.SendAsync(
                "jan@example.nl",
                It.Is<string>(subject => subject.Contains("Wijziging")),
                It.Is<string>(html => html.Contains("600") && html.Contains("817")),
                It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task Change_Within30DaysOfArrival_IsRefused()
        {
            SetArrival(29);

            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() => _sut.GetChangeQuoteAsync(ChangeRequest(shiftDays: 2), null));

            Assert.Equal(BookingService.CannotChangeMessage, exception.Message);
        }

        [Fact]
        public async Task Change_TooShortInHighSeason_IsRefused()
        {
            _seasonRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<Season>
            {
                new Season() { Name = "Zomervakantie", StartDate = Today.AddDays(50), EndDate = Today.AddDays(100), IsHighSeason = true },
            });
            ChangeBookingRequest request = ChangeRequest(shiftDays: 0);
            request.DepartureDate = request.ArrivalDate.AddDays(3);

            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() => _sut.GetChangeQuoteAsync(request, null));

            Assert.Contains("minimaal 7 nachten", exception.Message);
        }

        [Fact]
        public async Task Change_MissingCoTraveller_IsRefused()
        {
            ChangeBookingRequest request = ChangeRequest(shiftDays: 2);
            request.NumberOfGuests = 3;

            await Assert.ThrowsAsync<ArgumentException>(() => _sut.GetChangeQuoteAsync(request, null));
        }

        // ---------- Helpers ----------

        private static BookingAccessRequest Access() => new BookingAccessRequest() { Token = Token };

        private void SetArrival(int daysBeforeArrival)
        {
            _booking.ArrivalDate = Today.AddDays(daysBeforeArrival);
            _booking.DepartureDate = _booking.ArrivalDate.AddDays(7);
        }

        private void UnitsTaken(params Guid[] unitIds)
            => _bookingRepository
                .Setup(x => x.GetUnitIdsWithOverlappingBookingExcludingAsync(It.IsAny<List<Guid>>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<Guid>()))
                .ReturnsAsync(unitIds.ToList());

        // The same party, the stay shifted by a number of days.
        private ChangeBookingRequest ChangeRequest(int shiftDays)
            => new ChangeBookingRequest()
            {
                Token = Token,
                ArrivalDate = _booking.ArrivalDate.AddDays(shiftDays),
                DepartureDate = _booking.DepartureDate.AddDays(shiftDays),
                NumberOfGuests = 2,
                CoTravellers = [new TravellerRequest() { Name = "Els Jansen", BirthDate = new DateOnly(1987, 9, 12) }],
            };

        // Total 600: stay 480 + cleaning 110 + tourist tax 10; deposit 120 (20%).
        private Booking CreateBooking(int daysBeforeArrival)
            => new Booking()
            {
                Id = Guid.NewGuid(),
                BookingNumber = "K7M2XQ9P",
                AccessToken = Token,
                Name = "Jan Jansen",
                Email = "jan@example.nl",
                PhoneNumber = "0612345678",
                ArrivalDate = Today.AddDays(daysBeforeArrival),
                DepartureDate = Today.AddDays(daysBeforeArrival + 7),
                ArrivalTime = new TimeOnly(15, 0),
                DepartureTime = new TimeOnly(10, 0),
                NumberOfGuests = 2,
                AccommodationPrice = 480m,
                CleaningFee = 110m,
                TouristTax = 10m,
                TotalPrice = 600m,
                Deposit = 120m,
                AccommodationUnitId = _currentUnit.Id,
                AccommodationUnit = _currentUnit,
                Guests =
                [
                    new BookingGuest() { Name = "Jan Jansen", BirthDate = new DateOnly(1985, 5, 1), IsMainBooker = true },
                    new BookingGuest() { Name = "Els Jansen", BirthDate = new DateOnly(1987, 9, 12) },
                ],
            };
    }
}
