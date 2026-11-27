using Vakantiepark.Domain.Interfaces.Repositories;
using Vakantiepark.Domain.Interfaces.Helpers;
using Vakantiepark.Application.Interfaces;
using Vakantiepark.Application.Services;
using Vakantiepark.Shared.DTOs.Auth;
using Vakantiepark.Domain.Entities;
using Vakantiepark.Domain.Enums;
using Xunit;
using Moq;

namespace Vakantiepark.UnitTests
{
    public class AuthServiceTests
    {
        private const string CorrectPassword = "Medewerker123!";
        private const string CorrectCode = "123456";
        private const string CodeHash = "code-hash";
        private const string DeviceToken = "device-token";

        private readonly Mock<IUserRepository> _userRepository = new Mock<IUserRepository>();
        private readonly Mock<IBookingRepository> _bookingRepository = new Mock<IBookingRepository>();
        private readonly Mock<IPasswordHasher> _passwordHasher = new Mock<IPasswordHasher>();
        private readonly Mock<ITrustedDeviceRepository> _trustedDeviceRepository = new Mock<ITrustedDeviceRepository>();
        private readonly Mock<ITokenService> _tokenService = new Mock<ITokenService>();
        private readonly Mock<IEmailSender> _emailSender = new Mock<IEmailSender>();
        private readonly FixedTimeProvider _timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 7, 1, 12, 0, 0, TimeSpan.Zero));
        private readonly User _user = TestData.CreateUser();
        private readonly AuthService _sut;

        public AuthServiceTests()
        {
            _userRepository.Setup(x => x.GetByEmailAsync(_user.Email)).ReturnsAsync(_user);
            _passwordHasher.Setup(x => x.Verify(It.IsAny<string>(), _user.PasswordHash)).Returns(false);
            _passwordHasher.Setup(x => x.Verify(CorrectPassword, _user.PasswordHash)).Returns(true);
            _passwordHasher.Setup(x => x.Verify(It.IsAny<string>(), CodeHash)).Returns(false);
            _passwordHasher.Setup(x => x.Verify(CorrectCode, CodeHash)).Returns(true);
            _passwordHasher.Setup(x => x.Hash(It.IsAny<string>())).Returns(CodeHash);
            _tokenService.Setup(x => x.CreateStaffToken(It.IsAny<User>())).Returns(new TokenResponse() { Token = "staff-token" });
            _tokenService.Setup(x => x.CreateGuestToken(It.IsAny<Booking>())).Returns(new TokenResponse() { Token = "guest-token" });

            _sut = new AuthService(_userRepository.Object, _bookingRepository.Object, _passwordHasher.Object, _trustedDeviceRepository.Object, _tokenService.Object, _emailSender.Object, _timeProvider);
        }

        private DateOnly Today => DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);

        private DateTime Now => _timeProvider.GetUtcNow().UtcDateTime;

        private static LoginRequest Login(string email, string password, string? deviceToken = null)
            => new LoginRequest() { Email = email, Password = password, DeviceToken = deviceToken };

        private VerifyLoginCodeRequest Code(string code, bool rememberDevice = false)
            => new VerifyLoginCodeRequest() { Email = _user.Email, Code = code, RememberDevice = rememberDevice };

        private void GiveUserValidCode()
        {
            _user.LoginCodeHash = CodeHash;
            _user.LoginCodeExpiresAt = Now.AddMinutes(AuthService.LoginCodeValidMinutes);
        }

        [Fact]
        public async Task LoginAsync_Medewerker_EmailsCodeInsteadOfToken()
        {
            LoginResponse result = await _sut.LoginAsync(Login(_user.Email, CorrectPassword));

            Assert.True(result.RequiresCode);
            Assert.Null(result.Token);
            Assert.Equal(CodeHash, _user.LoginCodeHash);
            Assert.Equal(Now.AddMinutes(AuthService.LoginCodeValidMinutes), _user.LoginCodeExpiresAt);
            _emailSender.Verify(x => x.SendAsync(_user.Email, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            _tokenService.Verify(x => x.CreateStaffToken(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task LoginAsync_Medewerker_EmailsSixDigitCodeThatMatchesStoredHash()
        {
            string? hashedCode = null;
            string? textBody = null;
            _passwordHasher.Setup(x => x.Hash(It.IsAny<string>())).Callback<string>(code => hashedCode = code).Returns(CodeHash);
            _emailSender.Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string, string, string>((_, _, _, text) => textBody = text);

            await _sut.LoginAsync(Login(_user.Email, CorrectPassword));

            Assert.NotNull(hashedCode);
            Assert.Matches(@"^\d{6}$", hashedCode);
            Assert.Contains(hashedCode, textBody);
        }

        [Fact]
        public async Task LoginAsync_Beheerder_RequiresCode()
        {
            _user.Role = UserRole.Beheerder;

            LoginResponse result = await _sut.LoginAsync(Login(_user.Email, CorrectPassword));

            Assert.True(result.RequiresCode);
        }

        [Fact]
        public async Task LoginAsync_Schoonmaker_ReturnsTokenWithoutCode()
        {
            _user.Role = UserRole.Schoonmaker;

            LoginResponse result = await _sut.LoginAsync(Login(_user.Email, CorrectPassword));

            Assert.False(result.RequiresCode);
            Assert.Equal("staff-token", result.Token?.Token);
            _emailSender.Verify(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task LoginAsync_TrustedDevice_ReturnsTokenWithoutCode()
        {
            _trustedDeviceRepository.Setup(x => x.IsTrustedAsync(_user.Id, It.IsAny<string>(), Now)).ReturnsAsync(true);

            LoginResponse result = await _sut.LoginAsync(Login(_user.Email, CorrectPassword, DeviceToken));

            Assert.Equal("staff-token", result.Token?.Token);
            _emailSender.Verify(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task LoginAsync_UnknownOrExpiredDevice_RequiresCode()
        {
            _trustedDeviceRepository.Setup(x => x.IsTrustedAsync(_user.Id, It.IsAny<string>(), Now)).ReturnsAsync(false);

            LoginResponse result = await _sut.LoginAsync(Login(_user.Email, CorrectPassword, DeviceToken));

            Assert.True(result.RequiresCode);
        }

        [Fact]
        public async Task LoginAsync_TrustedDeviceButWrongPassword_ThrowsUnauthorizedAccessException()
        {
            _trustedDeviceRepository.Setup(x => x.IsTrustedAsync(_user.Id, It.IsAny<string>(), Now)).ReturnsAsync(true);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.LoginAsync(Login(_user.Email, "fout", DeviceToken)));
            _tokenService.Verify(x => x.CreateStaffToken(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task LoginAsync_EmailWithCapitalsAndSpaces_FindsUser()
        {
            LoginResponse result = await _sut.LoginAsync(Login("  Medewerker@DropUut.example ", CorrectPassword));

            Assert.True(result.RequiresCode);
        }

        [Fact]
        public async Task VerifyLoginCodeAsync_CorrectCode_ReturnsTokenAndClearsCode()
        {
            GiveUserValidCode();

            LoginResponse result = await _sut.VerifyLoginCodeAsync(Code(CorrectCode));

            Assert.Equal("staff-token", result.Token?.Token);
            Assert.Null(result.DeviceToken);
            Assert.Null(_user.LoginCodeHash);
            Assert.Null(_user.LoginCodeExpiresAt);
            _trustedDeviceRepository.Verify(x => x.AddAsync(It.IsAny<TrustedDevice>()), Times.Never);
        }

        [Fact]
        public async Task VerifyLoginCodeAsync_CodeUsedTwice_SecondTimeThrows()
        {
            GiveUserValidCode();
            await _sut.VerifyLoginCodeAsync(Code(CorrectCode));

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.VerifyLoginCodeAsync(Code(CorrectCode)));
        }

        [Fact]
        public async Task VerifyLoginCodeAsync_ExpiredCode_ThrowsUnauthorizedAccessException()
        {
            GiveUserValidCode();
            _timeProvider.Advance(TimeSpan.FromMinutes(AuthService.LoginCodeValidMinutes + 1));

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.VerifyLoginCodeAsync(Code(CorrectCode)));
            _tokenService.Verify(x => x.CreateStaffToken(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task VerifyLoginCodeAsync_NoCodeRequested_ThrowsUnauthorizedAccessException()
        {
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.VerifyLoginCodeAsync(Code(CorrectCode)));
        }

        [Fact]
        public async Task VerifyLoginCodeAsync_WrongCode_CountsAttemptAndThrows()
        {
            GiveUserValidCode();

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.VerifyLoginCodeAsync(Code("000000")));

            Assert.Equal(1, _user.FailedLoginCodeAttempts);
            Assert.Equal(CodeHash, _user.LoginCodeHash);
            _userRepository.Verify(x => x.UpdateAsync(_user), Times.Once);
        }

        [Fact]
        public async Task VerifyLoginCodeAsync_FifthWrongCode_InvalidatesCode()
        {
            GiveUserValidCode();

            for (int attempt = 0; attempt < AuthService.MaxFailedLoginCodeAttempts; attempt++)
                await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.VerifyLoginCodeAsync(Code("000000")));

            Assert.Null(_user.LoginCodeHash);
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.VerifyLoginCodeAsync(Code(CorrectCode)));
        }

        [Fact]
        public async Task VerifyLoginCodeAsync_RememberDevice_StoresHashedTokenFor30Days()
        {
            GiveUserValidCode();
            TrustedDevice? saved = null;
            _trustedDeviceRepository.Setup(x => x.AddAsync(It.IsAny<TrustedDevice>()))
                .Callback<TrustedDevice>(device => saved = device)
                .ReturnsAsync((TrustedDevice device) => device);

            LoginResponse result = await _sut.VerifyLoginCodeAsync(Code(CorrectCode, rememberDevice: true));

            Assert.NotNull(result.DeviceToken);
            Assert.NotNull(saved);
            Assert.Equal(_user.Id, saved.UserId);
            Assert.NotEqual(result.DeviceToken, saved.TokenHash);
            Assert.Equal(Now.AddDays(AuthService.TrustedDeviceDays), saved.ExpiresAt);
        }

        [Fact]
        public async Task LoginAsync_RememberedDevice_LooksUpSameHashThatWasStored()
        {
            GiveUserValidCode();
            TrustedDevice? saved = null;
            _trustedDeviceRepository.Setup(x => x.AddAsync(It.IsAny<TrustedDevice>()))
                .Callback<TrustedDevice>(device => saved = device)
                .ReturnsAsync((TrustedDevice device) => device);
            LoginResponse verified = await _sut.VerifyLoginCodeAsync(Code(CorrectCode, rememberDevice: true));
            _trustedDeviceRepository.Setup(x => x.IsTrustedAsync(_user.Id, saved!.TokenHash, Now)).ReturnsAsync(true);

            LoginResponse result = await _sut.LoginAsync(Login(_user.Email, CorrectPassword, verified.DeviceToken));

            Assert.False(result.RequiresCode);
            Assert.Equal("staff-token", result.Token?.Token);
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
