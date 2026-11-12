using Vakantiepark.UI.Application.Interfaces;
using Vakantiepark.UI.Authentication;
using Vakantiepark.Shared.DTOs.Auth;
using Vakantiepark.Shared.DTOs;

namespace Vakantiepark.UI.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IHttpService HttpService;
        private readonly JwtAuthenticationStateProvider AuthenticationStateProvider;

        public AuthService(IHttpService httpService, JwtAuthenticationStateProvider authenticationStateProvider)
        {
            HttpService = httpService;
            AuthenticationStateProvider = authenticationStateProvider;
        }

        public async Task<ApiResponse<TokenResponse>> LoginAsync(LoginRequest request)
            => await SignInAsync(await HttpService.PostAsync<TokenResponse, LoginRequest>("/Auth/Login", request));

        public async Task<ApiResponse<TokenResponse>> GuestLoginAsync(GuestLoginRequest request)
            => await SignInAsync(await HttpService.PostAsync<TokenResponse, GuestLoginRequest>("/Auth/Guest", request));

        public async Task LogoutAsync()
            => await AuthenticationStateProvider.SignOutAsync();

        private async Task<ApiResponse<TokenResponse>> SignInAsync(ApiResponse<TokenResponse> response)
        {
            if (response.Succeeded && response.Value is not null)
                await AuthenticationStateProvider.SignInAsync(response.Value.Token);

            return response;
        }
    }
}
