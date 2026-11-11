using Microsoft.IdentityModel.JsonWebTokens;
using Vakantiepark.Application.Services;
using Vakantiepark.Application.Settings;
using Vakantiepark.Shared.Constants;
using Vakantiepark.Shared.DTOs.Auth;
using Vakantiepark.Domain.Entities;
using Vakantiepark.Domain.Enums;
using System.Security.Claims;
using Xunit;

namespace Vakantiepark.UnitTests
{
    public class TokenServiceTests
    {
        private readonly FixedTimeProvider _timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 7, 1, 12, 0, 0, TimeSpan.Zero));
        private readonly TokenService _sut;

        public TokenServiceTests()
        {
            JwtSettings settings = new JwtSettings()
            {
                Issuer = "https://localhost:5001",
                Audience = "https://localhost:4200",
                Key = new string('k', 64),
                ExpiryMinutes = 480
            };

            _sut = new TokenService(settings, _timeProvider);
        }

        private static JsonWebToken Read(TokenResponse response)
            => new JsonWebTokenHandler().ReadJsonWebToken(response.Token);

        [Fact]
        public void CreateStaffToken_Beheerder_ContainsIdAndRole()
        {
            User user = TestData.CreateUser(x => x.Role = UserRole.Beheerder);

            JsonWebToken token = Read(_sut.CreateStaffToken(user));

            Assert.Equal(user.Id.ToString(), token.GetClaim(ClaimTypes.NameIdentifier).Value);
            Assert.Equal(Roles.Beheerder, token.GetClaim(ClaimTypes.Role).Value);
        }

        [Fact]
        public void CreateStaffToken_ExpiresAfterConfiguredMinutes()
        {
            TokenResponse response = _sut.CreateStaffToken(TestData.CreateUser());

            Assert.Equal(_timeProvider.GetUtcNow().UtcDateTime.AddMinutes(480), response.ExpiresAt);
        }

        [Fact]
        public void CreateGuestToken_ContainsBookingAndGuestRole()
        {
            Booking booking = TestData.CreateBooking(new DateOnly(2026, 7, 1));

            JsonWebToken token = Read(_sut.CreateGuestToken(booking));

            Assert.Equal(booking.Id.ToString(), token.GetClaim(ClaimNames.BookingId).Value);
            Assert.Equal(Roles.Gast, token.GetClaim(ClaimTypes.Role).Value);
        }

        [Fact]
        public void CreateGuestToken_ExpiresAtEndOfDepartureDay()
        {
            Booking booking = TestData.CreateBooking(new DateOnly(2026, 7, 1));

            TokenResponse response = _sut.CreateGuestToken(booking);

            Assert.Equal(booking.DepartureDate.ToDateTime(new TimeOnly(23, 59, 59), DateTimeKind.Utc), response.ExpiresAt);
        }
    }
}
