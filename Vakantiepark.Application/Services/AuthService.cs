using Vakantiepark.Domain.Interfaces.Repositories;
using Vakantiepark.Domain.Interfaces.Helpers;
using Vakantiepark.Application.Interfaces;
using Vakantiepark.Shared.DTOs.Auth;
using Vakantiepark.Domain.Entities;

namespace Vakantiepark.Application.Services
{
    public class AuthService : IAuthService
    {
        public const int MaxFailedLoginAttempts = 5;
        public const int LockoutMinutes = 15;

        private const string InvalidStaffLogin = "Het e-mailadres of wachtwoord is onjuist.";
        private const string InvalidGuestLogin = "Het boekingsnummer of e-mailadres is onjuist.";
        private const string LockedOut = "Je hebt te vaak een verkeerd wachtwoord ingevuld. Probeer het over 15 minuten opnieuw.";
        private const string InactiveAccount = "Je account is niet actief. Neem contact op met een beheerder.";
        private const string StayEnded = "Je verblijf is afgelopen, daarom kun je niet meer inloggen.";

        private readonly IUserRepository UserRepository;
        private readonly IBookingRepository BookingRepository;
        private readonly IPasswordHasher PasswordHasher;
        private readonly ITokenService TokenService;
        private readonly TimeProvider TimeProvider;

        public AuthService(IUserRepository userRepository, IBookingRepository bookingRepository, IPasswordHasher passwordHasher, ITokenService tokenService, TimeProvider timeProvider)
        {
            UserRepository = userRepository;
            BookingRepository = bookingRepository;
            PasswordHasher = passwordHasher;
            TokenService = tokenService;
            TimeProvider = timeProvider;
        }

        public async Task<TokenResponse> LoginAsync(LoginRequest request)
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

            return TokenService.CreateStaffToken(user);
        }

        public async Task<TokenResponse> GuestLoginAsync(GuestLoginRequest request)
        {
            Booking? booking = await BookingRepository.GetByBookingNumberAsync(request.BookingNumber.Trim().ToUpperInvariant());

            // One message for both a wrong number and a wrong email, so the response never confirms that a booking exists.
            if (booking is null || !string.Equals(booking.Email.Trim(), request.Email.Trim(), StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException(InvalidGuestLogin);

            if (booking.DepartureDate < Today())
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
            return booking is not null && booking.DepartureDate >= Today();
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

        private DateOnly Today()
            => DateOnly.FromDateTime(TimeProvider.GetUtcNow().UtcDateTime);
    }
}
