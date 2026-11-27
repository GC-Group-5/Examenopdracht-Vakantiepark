using Vakantiepark.Shared.DTOs.Auth;

namespace Vakantiepark.Application.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponse> LoginAsync(LoginRequest request);
        Task<LoginResponse> VerifyLoginCodeAsync(VerifyLoginCodeRequest request);
        Task<TokenResponse> GuestLoginAsync(GuestLoginRequest request);
        Task<bool> IsActiveUserAsync(Guid userId);
        Task<bool> IsActiveBookingAsync(Guid bookingId);
    }
}
