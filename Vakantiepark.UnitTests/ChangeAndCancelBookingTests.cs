using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Vakantiepark.Domain.Constants;
using Vakantiepark.Application.Interfaces;
using Vakantiepark.Application.Services;
using Vakantiepark.Application.Settings;
using Vakantiepark.Domain.Entities;
using Vakantiepark.Domain.Enums;
using Vakantiepark.Domain.Interfaces.Helpers;
using Vakantiepark.Domain.Interfaces.Repositories;
using Vakantiepark.Shared.Constants;
using Vakantiepark.Shared.DTOs.Booking;
using Vakantiepark.Shared.DTOs.Reservations;
using Xunit;

namespace Vakantiepark.UnitTests
{
    // US-12: cancelling and changing the guest's own booking; US-14: the same by staff, without the term.
    public class ChangeAndCancelBookingTests
    {
        private const string Token = "a3f1c2d4e5b6a7980112233445566778899aabbccddeeff00112233445566778";
        private static readonly Guid StaffUserId = Guid.NewGuid();
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
            _bookingRepository.Setup(x => x.GetByIdWithDetailsAsync(_booking.Id)).ReturnsAsync(() => _booking);
            _bookingRepository
                .Setup(x => x.GetUnitIdsWithOverlappingBookingAsync(It.IsAny<List<Guid>>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<Guid?>()))
                .ReturnsAsync(new List<Guid>());
            _bookingRepository.Setup(x => x.TryUpdateAsync(It.IsAny<Booking>(), It.IsAny<List<BookingGuest>>())).ReturnsAsync(true);
            _unitRepository
                .Setup(x => x.GetInServiceUnitsAsync(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
                .ReturnsAsync(() => new List<Accommodation> { _currentUnit, _otherUnit });
            _priceService
                .Setup(x => x.CalculateAsync(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
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
        public async Task Cancel_AlreadyCancelled_SaysSoAndNothingIsSaved()
        {
            _booking.DeletedAt = DateTime.UtcNow;

            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() => _sut.CancelAsync(Access(), null));

            Assert.Equal(BookingService.AlreadyCancelledMessage, exception.Message);
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

            _bookingRepository.Verify(x => x.GetUnitIdsWithOverlappingBookingAsync(
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
        public async Task Change_WheelchairAccessibleBooking_OnlyMovesToAnAccessibleUnit()
        {
            Accommodation accessibleUnit = TestData.CreateAccommodation("EL6-003", configure: unit => unit.Type = _type);
            _currentUnit.Features.Add(WheelchairAccess());
            accessibleUnit.Features.Add(WheelchairAccess());
            _booking.WheelchairAccessible = true;
            _unitRepository
                .Setup(x => x.GetInServiceUnitsAsync(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
                .ReturnsAsync(new List<Accommodation> { _currentUnit, _otherUnit, accessibleUnit });
            UnitsTaken(_currentUnit.Id);

            await _sut.ChangeAsync(ChangeRequest(shiftDays: 2), null);

            Assert.Equal(accessibleUnit.Id, _booking.AccommodationUnitId);
        }

        [Fact]
        public async Task Change_WheelchairAccessibleBookingAndNoAccessibleUnitFree_KeepsTheOldBooking()
        {
            _currentUnit.Features.Add(WheelchairAccess());
            _booking.WheelchairAccessible = true;
            UnitsTaken(_currentUnit.Id);

            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() => _sut.ChangeAsync(ChangeRequest(shiftDays: 2), null));

            Assert.Equal(BookingService.NoUnitForChangeMessage, exception.Message);
            Assert.Equal(_currentUnit.Id, _booking.AccommodationUnitId);
        }

        private static AccommodationFeature WheelchairAccess() => new AccommodationFeature()
        {
            Feature = new Feature() { Key = FeatureKeys.WheelchairAccessible, Name = "Rolstoeltoegankelijk" },
        };

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
        public async Task Change_AlreadyCancelled_SaysSo()
        {
            _booking.DeletedAt = DateTime.UtcNow;

            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() => _sut.GetChangeQuoteAsync(ChangeRequest(shiftDays: 2), null));

            Assert.Equal(BookingService.AlreadyCancelledMessage, exception.Message);
        }

        [Fact]
        public async Task Change_Within30DaysOfArrival_IsAllowedWhileAwaitingDeposit()
        {
            SetArrival(10);

            ChangeQuoteResponse quote = await _sut.GetChangeQuoteAsync(ChangeRequest(shiftDays: 2), null);

            Assert.Equal(600m, quote.OldTotalPrice);
        }

        [Fact]
        public async Task Change_AfterDepositIsPaid_IsRefusedAndNothingIsSaved()
        {
            SetArrival(10);
            _booking.Status = BookingState.Confirmed;

            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() => _sut.ChangeAsync(ChangeRequest(shiftDays: 2), null));

            Assert.Equal(BookingService.CannotChangeMessage, exception.Message);
            _bookingRepository.Verify(x => x.TryUpdateAsync(It.IsAny<Booking>(), It.IsAny<List<BookingGuest>>()), Times.Never);
            _emailSender.Verify(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetMine_AwaitingDeposit_CanChangeAndShowsStatus()
        {
            MyBookingResponse response = await _sut.GetMineByTokenAsync(Token);

            Assert.Equal(BookingStatuses.InAfwachtingVanAanbetaling, response.Status);
            Assert.True(response.CanChange);
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
        public async Task Change_CountsTheChildrenUnder12FromTheBirthDates()
        {
            ChangeBookingRequest request = ChangeRequest(shiftDays: 2);
            request.NumberOfGuests = 4;
            request.CoTravellers =
            [
                new TravellerRequest() { Name = "Els Jansen", BirthDate = new DateOnly(1987, 9, 12) },
                new TravellerRequest() { Name = "Tim Jansen", BirthDate = request.ArrivalDate.AddYears(-8) },
                new TravellerRequest() { Name = "Sanne Jansen", BirthDate = request.ArrivalDate.AddYears(-5) },
            ];

            await _sut.ChangeAsync(request, null);

            Assert.Equal(4, _booking.NumberOfGuests);
            Assert.Equal(2, _booking.NumberOfChildrenUnder12);
        }

        [Fact]
        public async Task Change_SavesTheFilledInTravellersAndSkipsEmptyRows()
        {
            ChangeBookingRequest request = ChangeRequest(shiftDays: 2);
            request.NumberOfGuests = 3;
            request.CoTravellers =
            [
                new TravellerRequest() { Name = "Els Jansen", BirthDate = new DateOnly(1987, 9, 12) },
                new TravellerRequest(),
            ];

            await _sut.ChangeAsync(request, null);

            _bookingRepository.Verify(x => x.TryUpdateAsync(_booking, It.Is<List<BookingGuest>>(guests =>
                guests.Count == 1 && guests[0].Name == "Els Jansen")), Times.Once);
        }

        [Fact]
        public async Task ChangeByStaffAsync_AfterTheGuestTerm_IsSavedAndLogged()
        {
            SetArrival(10);

            await _sut.ChangeByStaffAsync(_booking.Id, StaffChangeRequest(shiftDays: 2), StaffUserId);

            _bookingRepository.Verify(x => x.TryUpdateAsync(_booking, It.IsAny<List<BookingGuest>>()), Times.Once);
            BookingChange change = Assert.Single(_booking.Changes);
            Assert.Equal(StaffUserId, change.UserId);
            Assert.StartsWith("Gewijzigd:", change.Description);
        }

        [Fact]
        public async Task ChangeByStaffAsync_ChosenUnit_MovesTheBooking()
        {
            await _sut.ChangeByStaffAsync(_booking.Id, StaffChangeRequest(shiftDays: 2, unitCode: "EL6-002"), StaffUserId);

            Assert.Equal(_otherUnit.Id, _booking.AccommodationUnitId);
        }

        [Fact]
        public async Task ChangeByStaffAsync_ChosenUnitTaken_IsRefusedAndNothingIsSaved()
        {
            UnitsTaken(_otherUnit.Id);

            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _sut.ChangeByStaffAsync(_booking.Id, StaffChangeRequest(shiftDays: 2, unitCode: "EL6-002"), StaffUserId));

            Assert.Equal(BookingService.ChosenUnitNotFreeMessage, exception.Message);
            _bookingRepository.Verify(x => x.TryUpdateAsync(It.IsAny<Booking>(), It.IsAny<List<BookingGuest>>()), Times.Never);
        }

        [Fact]
        public async Task ChangeByStaffAsync_NewEmailAddress_IsUsedForTheChangeEmail()
        {
            StaffChangeBookingRequest request = StaffChangeRequest(shiftDays: 2);
            request.Email = "nieuw@example.nl";

            await _sut.ChangeByStaffAsync(_booking.Id, request, StaffUserId);

            Assert.Equal("nieuw@example.nl", _booking.Email);
            _emailSender.Verify(x => x.SendAsync("nieuw@example.nl", It.Is<string>(subject => subject.Contains("Wijziging")), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task ChangeByStaffAsync_UnknownBooking_ThrowsKeyNotFoundException()
        {
            await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.ChangeByStaffAsync(Guid.NewGuid(), StaffChangeRequest(shiftDays: 2), StaffUserId));
        }

        [Fact]
        public async Task GetCancellationQuoteForStaffAsync_ShowsTheSameFeeAsForTheGuest()
        {
            SetArrival(20);

            CancellationQuoteResponse quote = await _sut.GetCancellationQuoteForStaffAsync(_booking.Id);

            Assert.Equal(120m, quote.CancellationFee);
        }

        [Fact]
        public async Task GetCancellationQuoteForStaffAsync_FreeCancellation_UsesBookerWording()
        {
            SetArrival(60);

            CancellationQuoteResponse quote = await _sut.GetCancellationQuoteForStaffAsync(_booking.Id);

            Assert.Equal(
                "Je annuleert een boeking van meer dan 30 dagen voor aankomst. De boeker krijgt alles terug, inclusief de aanbetaling.",
                quote.Explanation);
        }

        [Fact]
        public async Task GetCancellationQuote_NoRefundPeriod_UsesRoleSpecificWordingAndKeepsTouristTaxRefund()
        {
            SetArrival(6);

            CancellationQuoteResponse guestQuote = await _sut.GetCancellationQuoteAsync(Access(), null);
            CancellationQuoteResponse staffQuote = await _sut.GetCancellationQuoteForStaffAsync(_booking.Id);

            Assert.Equal(590m, guestQuote.CancellationFee);
            Assert.Contains("Je betaalt het volledige verblijf", guestQuote.Explanation);
            Assert.Contains("toeristenbelasting", guestQuote.Explanation);
            Assert.Contains("De boeker betaalt het volledige verblijf", staffQuote.Explanation);
            Assert.Contains("wordt terugbetaald", staffQuote.Explanation);
        }

        // Between the two terms only the deposit is kept; the guest and staff each read it in their own words.
        [Fact]
        public async Task GetCancellationQuote_DepositPeriod_UsesRoleSpecificWordingWithTheTermsInOrder()
        {
            SetArrival(20);

            CancellationQuoteResponse guestQuote = await _sut.GetCancellationQuoteAsync(Access(), null);
            CancellationQuoteResponse staffQuote = await _sut.GetCancellationQuoteForStaffAsync(_booking.Id);

            Assert.StartsWith("Je annuleert 7 tot 30 dagen voor aankomst.", guestQuote.Explanation);
            Assert.StartsWith("Je annuleert een boeking tussen de 7 en 30 dagen voor aankomst. De boeker krijgt de rest terug", staffQuote.Explanation);
        }

        [Fact]
        public async Task CancelByStaffAsync_OnTheArrivalDay_CancelsLogsTheReasonAndEmails()
        {
            SetArrival(0);

            await _sut.CancelByStaffAsync(_booking.Id, new StaffCancelBookingRequest() { Reason = "Gast ziek" }, StaffUserId);

            _bookingRepository.Verify(x => x.CancelAsync(_booking, 590m), Times.Once);
            BookingChange change = Assert.Single(_booking.Changes);
            Assert.Equal("Gast ziek", change.Reason);
            Assert.Equal(StaffUserId, change.UserId);
            _emailSender.Verify(x => x.SendAsync("jan@example.nl", It.Is<string>(subject => subject.Contains("Annulering")), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task CancelByStaffAsync_WithoutReason_IsRefusedAndNothingIsSaved(string reason)
        {
            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _sut.CancelByStaffAsync(_booking.Id, new StaffCancelBookingRequest() { Reason = reason }, StaffUserId));

            Assert.Equal(BookingService.ReasonRequiredMessage, exception.Message);
            _bookingRepository.Verify(x => x.CancelAsync(It.IsAny<Booking>(), It.IsAny<decimal>()), Times.Never);
        }

        [Fact]
        public async Task CancelByStaffAsync_StayAlreadyEnded_IsRefusedAndNothingIsSaved()
        {
            SetArrival(-7);

            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _sut.CancelByStaffAsync(_booking.Id, new StaffCancelBookingRequest() { Reason = "Te laat" }, StaffUserId));

            Assert.Equal(BookingService.StayEndedMessage, exception.Message);
            _bookingRepository.Verify(x => x.CancelAsync(It.IsAny<Booking>(), It.IsAny<decimal>()), Times.Never);
            Assert.Empty(_booking.Changes);
        }

        [Fact]
        public async Task CancelByStaffAsync_AlreadyCancelled_SaysSo()
        {
            _booking.DeletedAt = DateTime.UtcNow;

            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _sut.CancelByStaffAsync(_booking.Id, new StaffCancelBookingRequest() { Reason = "Dubbel geboekt" }, StaffUserId));

            Assert.Equal(BookingService.AlreadyCancelledMessage, exception.Message);
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
                .Setup(x => x.GetUnitIdsWithOverlappingBookingAsync(It.IsAny<List<Guid>>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<Guid?>()))
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

        // The same party and main booker, the stay shifted; optionally a unit picked by staff.
        private StaffChangeBookingRequest StaffChangeRequest(int shiftDays, string? unitCode = null)
            => new StaffChangeBookingRequest()
            {
                Name = "Jan Jansen",
                Email = "jan@example.nl",
                PhoneNumber = "0612345678",
                BirthDate = new DateOnly(1985, 5, 1),
                ArrivalDate = _booking.ArrivalDate.AddDays(shiftDays),
                DepartureDate = _booking.DepartureDate.AddDays(shiftDays),
                NumberOfGuests = 2,
                AccommodationUnitCode = unitCode,
            };

        // Total 600: stay 480 + cleaning 110 + tourist tax 10; deposit 120 (20%).
        private Booking CreateBooking(int daysBeforeArrival)
            => new Booking()
            {
                Id = Guid.NewGuid(),
                BookingNumber = "K7M2XQ9P",
                Status = BookingState.AwaitingDeposit,
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
