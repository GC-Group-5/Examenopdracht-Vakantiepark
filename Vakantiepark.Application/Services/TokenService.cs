using Microsoft.IdentityModel.JsonWebTokens;
using Vakantiepark.Application.Interfaces;
using Vakantiepark.Application.Settings;
using Microsoft.IdentityModel.Tokens;
using Vakantiepark.Shared.Constants;
using Vakantiepark.Shared.DTOs.Auth;
using Vakantiepark.Domain.Entities;
using System.Security.Claims;
using System.Text;

namespace Vakantiepark.Application.Services
{
    public class TokenService : ITokenService
    {
        private readonly JwtSettings Settings;
        private readonly TimeProvider TimeProvider;

        public TokenService(JwtSettings settings, TimeProvider timeProvider)
        {
            Settings = settings;
            TimeProvider = timeProvider;
        }

        public TokenResponse CreateStaffToken(User user)
        {
            DateTime expiresAt = TimeProvider.GetUtcNow().UtcDateTime.AddMinutes(Settings.ExpiryMinutes);
            string role = user.Role.ToString();

            List<Claim> claims =
            [
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, role)
            ];

            return new TokenResponse() { Token = CreateToken(claims, expiresAt), ExpiresAt = expiresAt, Name = user.FullName, Role = role };
        }

        // A guest stays logged in until the end of the departure day, after that the stay is over.
        public TokenResponse CreateGuestToken(Booking booking)
        {
            DateTime expiresAt = booking.DepartureDate.ToDateTime(new TimeOnly(23, 59, 59), DateTimeKind.Utc);

            List<Claim> claims =
            [
                new Claim(ClaimNames.BookingId, booking.Id.ToString()),
                new Claim(ClaimNames.BookingNumber, booking.BookingNumber),
                new Claim(ClaimTypes.Name, booking.Name),
                new Claim(ClaimTypes.Email, booking.Email),
                new Claim(ClaimTypes.Role, Roles.Gast)
            ];

            return new TokenResponse() { Token = CreateToken(claims, expiresAt), ExpiresAt = expiresAt, Name = booking.Name, Role = Roles.Gast };
        }

        private string CreateToken(List<Claim> claims, DateTime expiresAt)
        {
            DateTime now = TimeProvider.GetUtcNow().UtcDateTime;
            SymmetricSecurityKey key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Settings.Key));

            SecurityTokenDescriptor descriptor = new SecurityTokenDescriptor()
            {
                Issuer = Settings.Issuer,
                Audience = Settings.Audience,
                Subject = new ClaimsIdentity(claims),
                IssuedAt = now,
                NotBefore = now,
                Expires = expiresAt,
                SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
            };

            return new JsonWebTokenHandler().CreateToken(descriptor);
        }
    }
}
