using Vakantiepark.Shared.DTOs.Auth;
using Vakantiepark.Shared.DTOs;

namespace Vakantiepark.UI.Application.Interfaces
{
    public interface IAuthService
    {
        Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request, bool rememberMe);
        Task<ApiResponse<LoginResponse>> VerifyLoginCodeAsync(VerifyLoginCodeRequest request);
        Task<ApiResponse<TokenResponse>> GuestLoginAsync(GuestLoginRequest request);
        Task LogoutAsync();
    }
}
