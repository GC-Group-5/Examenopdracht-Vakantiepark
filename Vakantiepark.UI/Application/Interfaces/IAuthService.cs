using Vakantiepark.Shared.DTOs.Auth;
using Vakantiepark.Shared.DTOs;

namespace Vakantiepark.UI.Application.Interfaces
{
    public interface IAuthService
    {
        Task<ApiResponse<TokenResponse>> LoginAsync(LoginRequest request);
        Task<ApiResponse<TokenResponse>> GuestLoginAsync(GuestLoginRequest request);
        Task LogoutAsync();
    }
}
