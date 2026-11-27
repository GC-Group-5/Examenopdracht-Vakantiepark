using Vakantiepark.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
using Vakantiepark.Domain.Interfaces.Helpers;
using Vakantiepark.Application.Interfaces;
using Vakantiepark.Application.Validations.Booking;
using Vakantiepark.Application.Services;
using Vakantiepark.Application.Settings;
using Vakantiepark.Shared.DTOs.Booking;
using Vakantiepark.Domain.Entities;
using Vakantiepark.Domain.Filters;
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
        private readonly Mock<ILogger<BookingService>> _logger = new Mock<ILogger<BookingService>>();
        private Booking? _savedBooking;
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
                .Setup(x => x.GetInServiceUnitsAsync(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
                .ReturnsAsync(() => new List<Accommodation> { _firstUnit, _secondUnit });
            _bookingRepository
                .Setup(x => x.GetUnitIdsWithOverlappingBookingAsync(It.IsAny<List<Guid>>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<Guid?>()))
                .ReturnsAsync(new List<Guid>());
            _bookingRepository
                .Setup(x => x.TryAddAsync(It.IsAny<Booking>()))
                .Callback<Booking>(booking => _savedBooking = booking)
                .ReturnsAsync(true);
            _priceService
                .Setup(x => x.CalculateAsync(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
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
                new FrontendSettings { BaseUrl = "https://localhost:4200" },
                _logger.Object);
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
        public async Task CreateAsync_NoTravellers_StoresOnlyTheMainBookerAndCountsTheChildren()
        {
            CreateBookingRequest request = TestData.CreateNewBookingRequest();

            await _sut.CreateAsync(request);

            _bookingRepository.Verify(x => x.TryAddAsync(It.Is<Booking>(booking =>
                booking.Guests.Count == 1
                && booking.Guests[0].IsMainBooker
                && booking.Guests[0].Name == "Jan Jansen"
                && booking.Guests[0].BirthDate == new DateOnly(1985, 5, 1)
                && booking.NumberOfGuests == 4
                && booking.NumberOfChildrenUnder12 == 0)), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_WithTravellers_StoresTheOnesWithANameAndSkipsEmptyRows()
        {
            CreateBookingRequest request = TestData.CreateNewBookingRequest(r => r.CoTravellers =
            [
                new TravellerRequest() { Name = "Els Jansen", BirthDate = new DateOnly(1987, 9, 12) },
                new TravellerRequest() { Name = "Piet Jansen" },
                new TravellerRequest(),
            ]);

            await _sut.CreateAsync(request);

            _bookingRepository.Verify(x => x.TryAddAsync(It.Is<Booking>(booking =>
                booking.Guests.Count == 3
                && booking.Guests[1].Name == "Els Jansen" && booking.Guests[1].BirthDate == new DateOnly(1987, 9, 12)
                && booking.Guests[2].Name == "Piet Jansen" && booking.Guests[2].BirthDate == null)), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_TravellersUnder12ByBirthDate_PassesThemToThePriceCalculation()
        {
            CreateBookingRequest request = TestData.CreateNewBookingRequest();
            request.CoTravellers =
            [
                new TravellerRequest() { Name = "Els Jansen", BirthDate = new DateOnly(1987, 9, 12) },
                new TravellerRequest() { Name = "Tim Jansen", BirthDate = request.ArrivalDate.AddYears(-8) },
                new TravellerRequest() { Name = "Sanne Jansen", BirthDate = request.ArrivalDate.AddYears(-5) },
            ];

            await _sut.CreateAsync(request);

            _priceService.Verify(x => x.CalculateAsync("eik-luxe-6", It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), 4, 2, 0), Times.Once);
            _bookingRepository.Verify(x => x.TryAddAsync(It.Is<Booking>(booking => booking.NumberOfChildrenUnder12 == 2)), Times.Once);
        }

        // Reception fills in the age at check-in; until then the traveller pays tourist tax.
        [Fact]
        public async Task CreateAsync_TravellerWithoutBirthDate_CountsAs12OrOlder()
        {
            CreateBookingRequest request = TestData.CreateNewBookingRequest();
            request.CoTravellers = [new TravellerRequest() { Name = "Tim Jansen" }, new TravellerRequest()];

            await _sut.CreateAsync(request);

            _priceService.Verify(x => x.CalculateAsync("eik-luxe-6", It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), 4, 0, 0), Times.Once);
        }

        // The age on the arrival date counts, not the age when booking.
        [Fact]
        public async Task CreateAsync_ChildTurning12BeforeArrival_PaysTouristTax()
        {
            CreateBookingRequest request = TestData.CreateNewBookingRequest();
            request.CoTravellers = [new TravellerRequest() { Name = "Tim Jansen", BirthDate = request.ArrivalDate.AddYears(-12) }];

            await _sut.CreateAsync(request);

            _priceService.Verify(x => x.CalculateAsync("eik-luxe-6", It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), 4, 0, 0), Times.Once);
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
        public async Task CreateAsync_ValidRequest_StoresTheCancellationLimitsOfTheParkSettings()
        {
            await _sut.CreateAsync(TestData.CreateNewBookingRequest());

            _bookingRepository.Verify(x => x.TryAddAsync(It.Is<Booking>(booking =>
                booking.FreeCancellationDaysBeforeArrival == 30
                && booking.NoRefundDaysBeforeArrival == 7)), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_ValidRequest_StoresSecurityDepositFromParkSettings()
        {
            BookingResponse response = await _sut.CreateAsync(TestData.CreateNewBookingRequest());

            Assert.Equal(250m, response.SecurityDeposit);
            Assert.Equal(250m, _savedBooking!.SecurityDeposit);
        }

        [Fact]
        public async Task CreateAsync_FirstUnitBooked_AssignsFreeUnit()
        {
            _bookingRepository
                .Setup(x => x.GetUnitIdsWithOverlappingBookingAsync(It.IsAny<List<Guid>>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<Guid?>()))
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
                .Setup(x => x.GetUnitIdsWithOverlappingBookingAsync(It.IsAny<List<Guid>>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<Guid?>()))
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
                .Setup(x => x.GetUnitIdsWithOverlappingBookingAsync(It.IsAny<List<Guid>>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<Guid?>()))
                .ReturnsAsync(new List<Guid> { _firstUnit.Id, _secondUnit.Id });

            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() => _sut.CreateAsync(TestData.CreateNewBookingRequest()));

            Assert.Equal(CreateBookingRequestValidation.NotAvailableMessage, exception.Message);
            _bookingRepository.Verify(x => x.TryAddAsync(It.IsAny<Booking>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_NoUnitFitsTheParty_ThrowsArgumentException()
        {
            _accommodationUnitRepository
                .Setup(x => x.GetInServiceUnitsAsync(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
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

            BookingQuoteResponse quote = await _sut.GetQuoteAsync("eik-luxe-6", arrival, arrival.AddDays(7), new AccommodationUnitFilter { NumberOfGuests = 4 }, 0, "Waterkant");

            Assert.Equal("Eik Luxe 6", quote.AccommodationTypeName);
            Assert.Equal("Waterkant", quote.Zone);
            Assert.Equal(new TimeOnly(15, 0), quote.ArrivalTime);
            Assert.Equal(6, quote.MaxPersons);
            Assert.Equal(817m, quote.Price.TotalPrice);
            Assert.Equal(163.40m, quote.Price.Deposit);
            Assert.Equal(30, quote.FreeCancellationDaysBeforeArrival);
            Assert.Equal(7, quote.NoRefundDaysBeforeArrival);
            _bookingRepository.Verify(x => x.TryAddAsync(It.IsAny<Booking>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_PetsRequested_UnitWithoutPetsIsSkipped()
        {
            _secondUnit.Features.Add(MaxPets(2));

            await _sut.CreateAsync(TestData.CreateNewBookingRequest(request => request.NumberOfPets = 1));

            _bookingRepository.Verify(x => x.TryAddAsync(It.Is<Booking>(booking => booking.AccommodationUnitId == _secondUnit.Id)), Times.Once);
            _bookingRepository.Verify(x => x.TryAddAsync(It.Is<Booking>(booking => booking.AccommodationUnitId == _firstUnit.Id)), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_MorePetsThanAnyUnitAllows_ThrowsMaximumMessage()
        {
            _secondUnit.Features.Add(MaxPets(2));

            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _sut.CreateAsync(TestData.CreateNewBookingRequest(request => request.NumberOfPets = 3)));

            Assert.Equal("Dit verblijfstype staat maximaal 2 huisdieren toe.", exception.Message);
            _bookingRepository.Verify(x => x.TryAddAsync(It.IsAny<Booking>()), Times.Never);
        }

        // US-54: more pets than the type allows must never reach the price calculation.
        [Fact]
        public async Task GetQuoteAsync_MorePetsThanAllowed_ThrowsAndCalculatesNoPrice()
        {
            _secondUnit.Features.Add(MaxPets(2));
            DateOnly arrival = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30);
            AccommodationUnitFilter filter = new AccommodationUnitFilter { NumberOfGuests = 2, NumberOfPets = 3 };

            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _sut.GetQuoteAsync("eik-luxe-6", arrival, arrival.AddDays(7), filter, 0, null));

            Assert.Equal("Dit verblijfstype staat maximaal 2 huisdieren toe.", exception.Message);
            _priceService.Verify(x => x.CalculateAsync(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task GetQuoteAsync_WheelchairAccessibleButNoAccessibleUnit_ThrowsNoPlaceMessage()
        {
            DateOnly arrival = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30);
            AccommodationUnitFilter filter = new AccommodationUnitFilter { NumberOfGuests = 2, WheelchairAccessible = true };

            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _sut.GetQuoteAsync("eik-luxe-6", arrival, arrival.AddDays(7), filter, 0, null));

            Assert.Contains("geen rolstoeltoegankelijke plek", exception.Message);
        }

        private static AccommodationFeature MaxPets(int maxPets) => new AccommodationFeature()
        {
            NumberValue = maxPets,
            Feature = new Feature() { Key = "maxPets", Name = "Maximaal aantal huisdieren" },
        };

        private static AccommodationFeature WheelchairAccess() => new AccommodationFeature()
        {
            Feature = new Feature() { Key = "wheelchairAccessible", Name = "Rolstoeltoegankelijk" },
        };

        [Fact]
        public async Task CreateAsync_PetsAndWheelchair_AreStoredOnTheBooking()
        {
            _secondUnit.Features.Add(MaxPets(2));
            _secondUnit.Features.Add(WheelchairAccess());

            BookingResponse response = await _sut.CreateAsync(TestData.CreateNewBookingRequest(request =>
            {
                request.NumberOfPets = 2;
                request.WheelchairAccessible = true;
            }));

            Assert.Equal(2, _savedBooking!.NumberOfPets);
            Assert.True(_savedBooking.WheelchairAccessible);
            Assert.Equal(2, response.NumberOfPets);
            Assert.True(response.WheelchairAccessible);
        }

        [Fact]
        public async Task CreateAsync_PetsAndWheelchair_AreInTheConfirmationEmail()
        {
            _secondUnit.Features.Add(MaxPets(2));
            _secondUnit.Features.Add(WheelchairAccess());

            await _sut.CreateAsync(TestData.CreateNewBookingRequest(request =>
            {
                request.NumberOfPets = 1;
                request.WheelchairAccessible = true;
            }));

            _emailSender.Verify(x => x.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.Is<string>(html => html.Contains("1 huisdier") && html.Contains("Rolstoeltoegankelijk</td><td style=\"padding:8px 0;\">ja")),
                It.Is<string>(text => text.Contains("Huisdieren:     1 huisdier") && text.Contains("Rolstoeltoegankelijk: ja"))), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_WithPetCleaningFee_EmailShowsTheFeeAsAPriceRow()
        {
            _secondUnit.Features.Add(MaxPets(2));
            _priceService
                .Setup(x => x.CalculateAsync(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(TestData.CreatePriceSpecification(price => price.PetCleaningFee = 25m));

            await _sut.CreateAsync(TestData.CreateNewBookingRequest(request => request.NumberOfPets = 1));

            _emailSender.Verify(x => x.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.Is<string>(html => html.Contains("Schoonmaak huisdieren</td><td align=\"right\" style=\"padding:6px 0;\">€") && html.Contains("25,00") && !html.Contains("&lt;tr")),
                It.Is<string>(text => text.Contains("Extra schoonmaakkosten: €") && text.Contains("25,00"))), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_NoPetsNoWheelchair_EmailSaysGeenAndNee()
        {
            await _sut.CreateAsync(TestData.CreateNewBookingRequest());

            _emailSender.Verify(x => x.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.Is<string>(text => text.Contains("Huisdieren:     geen") && text.Contains("Rolstoeltoegankelijk: nee"))), Times.Once);
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
                    && html.Contains("Jan Jansen")
                    && html.Contains("Aanbetaling (20%)")
                    && html.Contains("30 dagen")
                    && html.Contains("15:00")),
                It.Is<string>(text => text.Contains("K7M2XQ9P") && text.Contains("ANNULERINGSVOORWAARDEN"))), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_ValidRequest_EmailContainsPersonalMyBookingLink()
        {
            await _sut.CreateAsync(TestData.CreateNewBookingRequest());

            string link = $"https://localhost:4200/mijn-boeking/{_savedBooking!.AccessToken}";
            Assert.Matches("^[0-9a-f]{64}$", _savedBooking.AccessToken);
            _emailSender.Verify(x => x.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.Is<string>(html => html.Contains(link)),
                It.Is<string>(text => text.Contains(link))), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_AllUnitsBooked_SendsNoEmail()
        {
            _bookingRepository
                .Setup(x => x.GetUnitIdsWithOverlappingBookingAsync(It.IsAny<List<Guid>>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<Guid?>()))
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
            _logger.Verify(x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<InvalidOperationException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_OneGuest_EmailSaysOnePerson()
        {
            CreateBookingRequest request = TestData.CreateNewBookingRequest();
            request.NumberOfGuests = 1;

            await _sut.CreateAsync(request);

            _emailSender.Verify(x => x.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.Is<string>(html => html.Contains("1 persoon:")),
                It.Is<string>(text => text.Contains("1 persoon:"))), Times.Once);
        }
    }
}
