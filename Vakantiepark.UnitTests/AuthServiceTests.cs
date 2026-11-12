using Vakantiepark.Domain.Interfaces.Repositories;
using Vakantiepark.Domain.Interfaces.Helpers;
using Vakantiepark.Application.Interfaces;
using Vakantiepark.Application.Services;
using Vakantiepark.Shared.DTOs.Auth;
using Vakantiepark.Domain.Entities;
using Xunit;
using Moq;

namespace Vakantiepark.UnitTests
{
    public class AuthServiceTests
    {
        private const string CorrectPassword = "Medewerker123!";

        private readonly Mock<IUserRepository> _userRepository = new Mock<IUserRepository>();
        private readonly Mock<IBookingRepository> _bookingRepository = new Mock<IBookingRepository>();
        private readonly Mock<IPasswordHasher> _passwordHasher = new Mock<IPasswordHasher>();
        private readonly Mock<ITokenService> _tokenService = new Mock<ITokenService>();
        private readonly FixedTimeProvider _timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 7, 1, 12, 0, 0, TimeSpan.Zero));
        private readonly User _user = TestData.CreateUser();
        private readonly AuthService _sut;

        public AuthServiceTests()
        {
            _userRepository.Setup(x => x.GetByEmailAsync(_user.Email)).ReturnsAsync(_user);
            _passwordHasher.Setup(x => x.Verify(It.IsAny<string>(), _user.PasswordHash)).Returns(false);
            _passwordHasher.Setup(x => x.Verify(CorrectPassword, _user.PasswordHash)).Returns(true);
            _tokenService.Setup(x => x.CreateStaffToken(It.IsAny<User>())).Returns(new TokenResponse() { Token = "staff-token" });
            _tokenService.Setup(x => x.CreateGuestToken(It.IsAny<Booking>())).Returns(new TokenResponse() { Token = "guest-token" });

            _sut = new AuthService(_userRepository.Object, _bookingRepository.Object, _passwordHasher.Object, _tokenService.Object, _timeProvider);
        }

        private DateOnly Today => DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);

        private static LoginRequest Login(string email, string password)
            => new LoginRequest() { Email = email, Password = password };

        [Fact]
        public async Task LoginAsync_CorrectPassword_ReturnsStaffToken()
        {
            TokenResponse result = await _sut.LoginAsync(Login(_user.Email, CorrectPassword));

            Assert.Equal("staff-token", result.Token);
            _tokenService.Verify(x => x.CreateStaffToken(_user), Times.Once);
        }

        [Fact]
        public async Task LoginAsync_EmailWithCapitalsAndSpaces_FindsUser()
        {
            TokenResponse result = await _sut.LoginAsync(Login("  Medewerker@DropUut.example ", CorrectPassword));

            Assert.Equal("staff-token", result.Token);
        }

        [Fact]
        public async Task LoginAsync_UnknownEmail_ThrowsUnauthorizedAccessException()
        {
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.LoginAsync(Login("onbekend@dropuut.example", CorrectPassword)));
        }

        [Fact]
        public async Task LoginAsync_WrongPassword_CountsFailedAttemptAndThrows()
        {
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.LoginAsync(Login(_user.Email, "fout")));

            Assert.Equal(1, _user.FailedLoginAttempts);
            _userRepository.Verify(x => x.UpdateAsync(_user), Times.Once);
            _tokenService.Verify(x => x.CreateStaffToken(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task LoginAsync_FifthWrongPassword_LocksAccountFor15Minutes()
        {
            for (int attempt = 0; attempt < AuthService.MaxFailedLoginAttempts; attempt++)
                await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.LoginAsync(Login(_user.Email, "fout")));

            Assert.Equal(_timeProvider.GetUtcNow().UtcDateTime.AddMinutes(AuthService.LockoutMinutes), _user.LockoutEnd);
        }

        [Fact]
        public async Task LoginAsync_LockedAccount_RefusesCorrectPassword()
        {
            _user.LockoutEnd = _timeProvider.GetUtcNow().UtcDateTime.AddMinutes(10);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.LoginAsync(Login(_user.Email, CorrectPassword)));
            _tokenService.Verify(x => x.CreateStaffToken(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task LoginAsync_LockoutExpired_AllowsLoginAndResetsLockout()
        {
            _user.FailedLoginAttempts = 3;
            _user.LockoutEnd = _timeProvider.GetUtcNow().UtcDateTime.AddMinutes(10);
            _timeProvider.Advance(TimeSpan.FromMinutes(11));

            await _sut.LoginAsync(Login(_user.Email, CorrectPassword));

            Assert.Equal(0, _user.FailedLoginAttempts);
            Assert.Null(_user.LockoutEnd);
        }

        [Fact]
        public async Task LoginAsync_InactiveUser_ThrowsUnauthorizedAccessException()
        {
            _user.IsActive = false;

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.LoginAsync(Login(_user.Email, CorrectPassword)));
        }

        [Fact]
        public async Task GuestLoginAsync_CorrectNumberAndEmail_ReturnsGuestToken()
        {
            Booking booking = TestData.CreateBooking(Today);
            _bookingRepository.Setup(x => x.GetByBookingNumberAsync(booking.BookingNumber)).ReturnsAsync(booking);

            TokenResponse result = await _sut.GuestLoginAsync(new GuestLoginRequest() { BookingNumber = " dou-7kq3m9xp ", Email = "JAN@example.nl" });

            Assert.Equal("guest-token", result.Token);
            _tokenService.Verify(x => x.CreateGuestToken(booking), Times.Once);
        }

        [Fact]
        public async Task GuestLoginAsync_WrongEmail_ThrowsUnauthorizedAccessException()
        {
            Booking booking = TestData.CreateBooking(Today);
            _bookingRepository.Setup(x => x.GetByBookingNumberAsync(booking.BookingNumber)).ReturnsAsync(booking);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.GuestLoginAsync(new GuestLoginRequest() { BookingNumber = booking.BookingNumber, Email = "iemand@anders.nl" }));
            _tokenService.Verify(x => x.CreateGuestToken(It.IsAny<Booking>()), Times.Never);
        }

        [Fact]
        public async Task GuestLoginAsync_UnknownBookingNumber_ThrowsUnauthorizedAccessException()
        {
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.GuestLoginAsync(new GuestLoginRequest() { BookingNumber = "DOU-BESTAATN", Email = "jan@example.nl" }));
        }

        [Fact]
        public async Task GuestLoginAsync_StayEnded_ThrowsUnauthorizedAccessException()
        {
            Booking booking = TestData.CreateBooking(Today, x => { x.ArrivalDate = Today.AddDays(-8); x.DepartureDate = Today.AddDays(-1); });
            _bookingRepository.Setup(x => x.GetByBookingNumberAsync(booking.BookingNumber)).ReturnsAsync(booking);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.GuestLoginAsync(new GuestLoginRequest() { BookingNumber = booking.BookingNumber, Email = booking.Email }));
        }

        [Fact]
        public async Task IsActiveBookingAsync_DepartureToday_ReturnsTrue()
        {
            Booking booking = TestData.CreateBooking(Today, x => x.DepartureDate = Today);
            _bookingRepository.Setup(x => x.GetByIdAsync(booking.Id)).ReturnsAsync(booking);

            Assert.True(await _sut.IsActiveBookingAsync(booking.Id));
        }

        [Fact]
        public async Task IsActiveUserAsync_DeactivatedUser_ReturnsFalse()
        {
            _user.IsActive = false;
            _userRepository.Setup(x => x.GetByIdAsync(_user.Id)).ReturnsAsync(_user);

            Assert.False(await _sut.IsActiveUserAsync(_user.Id));
        }
    }
}
