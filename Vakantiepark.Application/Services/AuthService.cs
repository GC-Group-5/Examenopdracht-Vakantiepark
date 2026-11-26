using System.Security.Cryptography;
using System.Text;
using Vakantiepark.Application.Extensions;
using Vakantiepark.Application.Helpers;
using Vakantiepark.Domain.Interfaces.Repositories;
using Vakantiepark.Domain.Interfaces.Helpers;
using Vakantiepark.Application.Interfaces;
using Vakantiepark.Shared.DTOs.Auth;
using Vakantiepark.Domain.Entities;
using Vakantiepark.Domain.Enums;

namespace Vakantiepark.Application.Services
{
    public class AuthService : IAuthService
    {
        public const int MaxFailedLoginAttempts = 5;
        public const int LockoutMinutes = 15;
        public const int LoginCodeLength = 6;
        public const int LoginCodeValidMinutes = 10;
        public const int MaxFailedLoginCodeAttempts = 5;
        public const int TrustedDeviceDays = 30;
        public const int DeviceTokenBytes = 32;

        private const string LoginCodeDigits = "0123456789";

        private const string InvalidStaffLogin = "Het e-mailadres of wachtwoord is onjuist.";
        private const string InvalidGuestLogin = "Boeking niet gevonden. Controleer je boekingsnummer en e-mailadres.";
        private const string LockedOut = "Je hebt te vaak een verkeerd wachtwoord ingevuld. Probeer het over 15 minuten opnieuw.";
        private const string InactiveAccount = "Je account is niet actief. Neem contact op met een beheerder.";
        private const string StayEnded = "Je verblijf is afgelopen, daarom kun je niet meer inloggen.";
        private const string InvalidLoginCode = "De code is onjuist. Controleer de code in je e-mail.";
        private const string LoginCodeExpired = "De code is verlopen. Log opnieuw in om een nieuwe code te krijgen.";
        private const string TooManyLoginCodeAttempts = "Je hebt te vaak een verkeerde code ingevuld. Log opnieuw in om een nieuwe code te krijgen.";
        private const string LoginCodeSubject = "Je inlogcode voor Vakantiepark D'r op Uut";

        private readonly IUserRepository UserRepository;
        private readonly IBookingRepository BookingRepository;
        private readonly IPasswordHasher PasswordHasher;
        private readonly ITrustedDeviceRepository TrustedDeviceRepository;
        private readonly ITokenService TokenService;
        private readonly IEmailSender EmailSender;
        private readonly TimeProvider TimeProvider;

        public AuthService(IUserRepository userRepository, IBookingRepository bookingRepository, IPasswordHasher passwordHasher, ITrustedDeviceRepository trustedDeviceRepository, ITokenService tokenService, IEmailSender emailSender, TimeProvider timeProvider)
        {
            UserRepository = userRepository;
            BookingRepository = bookingRepository;
            PasswordHasher = passwordHasher;
            TrustedDeviceRepository = trustedDeviceRepository;
            TokenService = tokenService;
            EmailSender = emailSender;
            TimeProvider = timeProvider;
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            User user = await UserRepository.GetByEmailAsync(request.Email.Trim().ToLowerInvariant())
                ?? throw new UnauthorizedAccessException(InvalidStaffLogin);

            DateTime now = TimeProvider.GetUtcNow().UtcDateTime;
            if (user.LockoutEnd > now)
                throw new UnauthorizedAccessException(LockedOut);

            if (!PasswordHasher.Verify(request.Password, user.PasswordHash))
            {
                bool lockedOut = await RegisterFailedAttemptAsync(user, now);
                throw new UnauthorizedAccessException(lockedOut ? LockedOut : InvalidStaffLogin);
            }

            if (!user.IsActive)
                throw new UnauthorizedAccessException(InactiveAccount);

            if (user.FailedLoginAttempts > 0 || user.LockoutEnd is not null)
            {
                user.FailedLoginAttempts = 0;
                user.LockoutEnd = null;
                await UserRepository.UpdateAsync(user);
            }

            if (!RequiresLoginCode(user) || await IsTrustedDeviceAsync(user, request.DeviceToken, now))
                return new LoginResponse() { Token = TokenService.CreateStaffToken(user) };

            await SendLoginCodeAsync(user, now);
            return new LoginResponse() { RequiresCode = true };
        }

        public async Task<LoginResponse> VerifyLoginCodeAsync(VerifyLoginCodeRequest request)
        {
            User? user = await UserRepository.GetByEmailAsync(request.Email.Trim().ToLowerInvariant());

            DateTime now = TimeProvider.GetUtcNow().UtcDateTime;
            if (user is null || user.LoginCodeHash is null || user.LoginCodeExpiresAt <= now)
                throw new UnauthorizedAccessException(LoginCodeExpired);

            if (!PasswordHasher.Verify(request.Code, user.LoginCodeHash))
            {
                user.FailedLoginCodeAttempts++;

                bool tooManyAttempts = user.FailedLoginCodeAttempts >= MaxFailedLoginCodeAttempts;
                if (tooManyAttempts)
                    ClearLoginCode(user);

                await UserRepository.UpdateAsync(user);
                throw new UnauthorizedAccessException(tooManyAttempts ? TooManyLoginCodeAttempts : InvalidLoginCode);
            }

            ClearLoginCode(user);
            await UserRepository.UpdateAsync(user);

            if (!user.IsActive)
                throw new UnauthorizedAccessException(InactiveAccount);

            LoginResponse response = new LoginResponse() { Token = TokenService.CreateStaffToken(user) };
            if (request.RememberDevice)
                response.DeviceToken = await TrustDeviceAsync(user, now);

            return response;
        }

        public async Task<TokenResponse> GuestLoginAsync(GuestLoginRequest request)
        {
            Booking? booking = await BookingRepository.GetByBookingNumberAsync(request.BookingNumber.Trim().ToUpperInvariant());

            // One message for both a wrong number and a wrong email, so the response never confirms that a booking exists.
            if (booking is null || !string.Equals(booking.Email.Trim(), request.Email.Trim(), StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException(InvalidGuestLogin);

            if (booking.DepartureDate < TimeProvider.GetToday())
                throw new UnauthorizedAccessException(StayEnded);

            return TokenService.CreateGuestToken(booking);
        }

        public async Task<bool> IsActiveUserAsync(Guid userId)
        {
            User? user = await UserRepository.GetByIdAsync(userId);
            return user is not null && user.IsActive;
        }

        public async Task<bool> IsActiveBookingAsync(Guid bookingId)
        {
            Booking? booking = await BookingRepository.GetByIdAsync(bookingId);
            return booking is not null && booking.DepartureDate >= TimeProvider.GetToday();
        }

        private async Task<bool> RegisterFailedAttemptAsync(User user, DateTime now)
        {
            user.FailedLoginAttempts++;

            bool lockedOut = user.FailedLoginAttempts >= MaxFailedLoginAttempts;
            if (lockedOut)
            {
                user.FailedLoginAttempts = 0;
                user.LockoutEnd = now.AddMinutes(LockoutMinutes);
            }

            await UserRepository.UpdateAsync(user);
            return lockedOut;
        }

        // Schoonmakers only see a list without personal data, so they skip the extra step.
        private static bool RequiresLoginCode(User user)
            => user.Role is UserRole.Beheerder or UserRole.Medewerker;

        private async Task<bool> IsTrustedDeviceAsync(User user, string? deviceToken, DateTime now)
            => !string.IsNullOrWhiteSpace(deviceToken)
                && await TrustedDeviceRepository.IsTrustedAsync(user.Id, HashDeviceToken(deviceToken), now);

        private async Task SendLoginCodeAsync(User user, DateTime now)
        {
            string code = RandomNumberGenerator.GetString(LoginCodeDigits, LoginCodeLength);

            user.LoginCodeHash = PasswordHasher.Hash(code);
            user.LoginCodeExpiresAt = now.AddMinutes(LoginCodeValidMinutes);
            user.FailedLoginCodeAttempts = 0;
            await UserRepository.UpdateAsync(user);

            RenderedEmail email = EmailTemplate.Render(EmailTemplate.LoginCode, new Dictionary<string, string>
            {
                ["Name"] = user.FullName,
                ["Code"] = code,
                ["ValidMinutes"] = LoginCodeValidMinutes.ToString()
            });

            await EmailSender.SendAsync(user.Email, LoginCodeSubject, email.HtmlBody, email.TextBody);
        }

        private async Task<string> TrustDeviceAsync(User user, DateTime now)
        {
            string deviceToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(DeviceTokenBytes)).ToLowerInvariant();

            await TrustedDeviceRepository.AddAsync(new TrustedDevice()
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = HashDeviceToken(deviceToken),
                ExpiresAt = now.AddDays(TrustedDeviceDays)
            });

            return deviceToken;
        }

        private static void ClearLoginCode(User user)
        {
            user.LoginCodeHash = null;
            user.LoginCodeExpiresAt = null;
            user.FailedLoginCodeAttempts = 0;
        }

        // The device token is long and random, so a fast hash is enough and lets the database look it up directly.
        // The short login code does need a slow hash (BCrypt via IPasswordHasher).
        private static string HashDeviceToken(string deviceToken)
            => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(deviceToken))).ToLowerInvariant();
    }
}
