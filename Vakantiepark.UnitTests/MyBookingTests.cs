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
    // US-11: a guest only ever sees his own booking, via the secret link or booking number + email.
    public class MyBookingTests
    {
        private const string Token = "a3f1c2d4e5b6a7980112233445566778899aabbccddeeff00112233445566778";
        private static readonly DateOnly Today = new DateOnly(2027, 4, 1);

        private readonly Mock<IBookingRepository> _bookingRepository = new Mock<IBookingRepository>();
        private readonly Booking _booking;
        private readonly BookingService _sut;

        public MyBookingTests()
        {
            _booking = CreateBooking();

            _bookingRepository
                .Setup(x => x.GetByAccessTokenAsync(Token))
                .ReturnsAsync(() => _booking);
            _bookingRepository
                .Setup(x => x.GetByIdWithDetailsAsync(_booking.Id))
                .ReturnsAsync(() => _booking);

            _sut = new BookingService(
                _bookingRepository.Object,
                new Mock<IAccommodationUnitRepository>().Object,
                new Mock<IAccommodationTypeRepository>().Object,
                new Mock<IPriceService>().Object,
                new Mock<IBookingNumberGenerator>().Object,
                new Mock<IRepository<Season>>().Object,
                CreateParkSettingsRepository(),
                new FixedTimeProvider(new DateTimeOffset(Today.ToDateTime(new TimeOnly(9, 0)), TimeSpan.Zero)),
                new Mock<IEmailSender>().Object,
                new FrontendSettings { BaseUrl = "https://localhost:4200" },
                NullLogger<BookingService>.Instance);
        }

        [Fact]
        public async Task GetMineByToken_ValidToken_ReturnsDetailsPriceAndStatus()
        {
            MyBookingResponse response = await _sut.GetMineByTokenAsync(Token);

            Assert.Equal("K7M2XQ9P", response.BookingNumber);
            Assert.Equal("Eik Luxe 6", response.AccommodationTypeName);
            Assert.Equal(600m, response.TotalPrice);
            Assert.Equal(120m, response.Deposit);
            Assert.Equal("Bevestigd", response.Status);
            Assert.Equal(new[] { "Jan Jansen", "Els Jansen" }, response.Guests.Select(guest => guest.Name));
            Assert.True(response.Guests[0].IsMainBooker);
        }

        [Fact]
        public async Task GetMineByToken_ShowsPetsAndZone()
        {
            _booking.NumberOfPets = 2;

            MyBookingResponse response = await _sut.GetMineByTokenAsync(Token);

            Assert.Equal(2, response.NumberOfPets);
            Assert.Equal(_booking.AccommodationUnit.Zone.Name, response.Zone);
        }

        [Fact]
        public async Task GetMineByToken_UnknownOrChangedToken_ThrowsInvalidLink()
        {
            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _sut.GetMineByTokenAsync(Token[..^1] + "0"));

            Assert.Equal(BookingService.InvalidLinkMessage, exception.Message);
        }

        [Fact]
        public async Task GetMineByToken_OnDepartureDay_StillWorks()
        {
            _booking.DepartureDate = Today;

            MyBookingResponse response = await _sut.GetMineByTokenAsync(Token);

            Assert.Equal("K7M2XQ9P", response.BookingNumber);
        }

        [Fact]
        public async Task GetMineByToken_AfterDepartureDay_ThrowsExpiredLink()
        {
            _booking.DepartureDate = Today.AddDays(-1);

            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() => _sut.GetMineByTokenAsync(Token));

            Assert.Equal(BookingService.ExpiredLinkMessage, exception.Message);
        }

        // Booking number + email is the guest login (/Auth/Guest, tested in the auth tests); the booking id
        // then comes from the guest token.
        [Fact]
        public async Task GetMine_BookingIdFromGuestToken_ReturnsBooking()
        {
            MyBookingResponse response = await _sut.GetMineAsync(_booking.Id);

            Assert.Equal("K7M2XQ9P", response.BookingNumber);
        }

        [Fact]
        public async Task GetMine_UnknownBookingId_ThrowsNotFound()
        {
            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() => _sut.GetMineAsync(Guid.NewGuid()));

            Assert.Equal(BookingService.NotFoundMessage, exception.Message);
        }

        [Fact]
        public async Task GetMine_AfterDeparture_HasStatusAfgerond()
        {
            _booking.ArrivalDate = Today.AddDays(-7);
            _booking.DepartureDate = Today.AddDays(-1);

            MyBookingResponse response = await _sut.GetMineAsync(_booking.Id);

            Assert.Equal("Afgerond", response.Status);
        }

        [Fact]
        public async Task GetMineByToken_CancelledBooking_HasStatusGeannuleerd()
        {
            _booking.DeletedAt = DateTime.UtcNow;

            MyBookingResponse response = await _sut.GetMineByTokenAsync(Token);

            Assert.Equal("Geannuleerd", response.Status);
        }

        [Fact]
        public void MyBookingResponse_ContainsNoIdOrAccessToken()
        {
            string[] properties = typeof(MyBookingResponse).GetProperties().Select(property => property.Name).ToArray();

            Assert.DoesNotContain("Id", properties);
            Assert.DoesNotContain("AccessToken", properties);
        }

        private static IRepository<ParkSettings> CreateParkSettingsRepository()
        {
            Mock<IRepository<ParkSettings>> repository = new Mock<IRepository<ParkSettings>>();
            repository.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<ParkSettings> { TestData.CreateParkSettings() });
            return repository.Object;
        }

        private static Booking CreateBooking()
            => new Booking()
            {
                Id = Guid.NewGuid(),
                BookingNumber = "K7M2XQ9P",
                AccessToken = Token,
                Name = "Jan Jansen",
                Email = "jan@example.nl",
                PhoneNumber = "0612345678",
                ArrivalDate = Today.AddDays(10),
                DepartureDate = Today.AddDays(15),
                ArrivalTime = new TimeOnly(15, 0),
                DepartureTime = new TimeOnly(10, 0),
                NumberOfGuests = 2,
                AccommodationPrice = 480m,
                CleaningFee = 110m,
                TouristTax = 10m,
                TotalPrice = 600m,
                Deposit = 120m,
                AccommodationUnit = TestData.CreateAccommodation("EL6-001", configure: unit =>
                    unit.Type = TestData.CreateAccommodationType(type => type.Name = "Eik Luxe 6")),
                Guests =
                [
                    new BookingGuest() { Name = "Els Jansen", BirthDate = new DateOnly(1987, 9, 12) },
                    new BookingGuest() { Name = "Jan Jansen", BirthDate = new DateOnly(1985, 5, 1), IsMainBooker = true },
                ],
            };
    }
}
