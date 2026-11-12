using Vakantiepark.Shared.DTOs.Auth;

namespace Vakantiepark.Application.Interfaces
{
    public interface IAuthService
    {
        Task<TokenResponse> LoginAsync(LoginRequest request);
        Task<TokenResponse> GuestLoginAsync(GuestLoginRequest request);
        Task<bool> IsActiveUserAsync(Guid userId);
        Task<bool> IsActiveBookingAsync(Guid bookingId);
    }
}
