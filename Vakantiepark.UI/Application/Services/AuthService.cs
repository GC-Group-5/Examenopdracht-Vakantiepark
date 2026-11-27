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
        private readonly TrustedDeviceStorage TrustedDeviceStorage;

        public AuthService(IHttpService httpService, JwtAuthenticationStateProvider authenticationStateProvider, TrustedDeviceStorage trustedDeviceStorage)
        {
            HttpService = httpService;
            AuthenticationStateProvider = authenticationStateProvider;
            TrustedDeviceStorage = trustedDeviceStorage;
        }

        public async Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request, bool rememberMe)
        {
            request.DeviceToken = await TrustedDeviceStorage.GetAsync(request.Email);
            return await SignInAsync(await HttpService.PostAsync<LoginResponse, LoginRequest>("/Auth/Login", request), rememberMe);
        }

        public async Task<ApiResponse<LoginResponse>> VerifyLoginCodeAsync(VerifyLoginCodeRequest request)
        {
            ApiResponse<LoginResponse> response = await HttpService.PostAsync<LoginResponse, VerifyLoginCodeRequest>("/Auth/VerifyCode", request);
            if (response.Succeeded && response.Value?.DeviceToken is not null)
                await TrustedDeviceStorage.SetAsync(request.Email, response.Value.DeviceToken);

            return await SignInAsync(response, request.RememberDevice);
        }

        public async Task<ApiResponse<TokenResponse>> GuestLoginAsync(GuestLoginRequest request)
        {
            ApiResponse<TokenResponse> response = await HttpService.PostAsync<TokenResponse, GuestLoginRequest>("/Auth/Guest", request);
            if (response.Succeeded && response.Value is not null)
                await AuthenticationStateProvider.SignInAsync(response.Value.Token, rememberMe: true);

            return response;
        }

        public async Task LogoutAsync()
            => await AuthenticationStateProvider.SignOutAsync();

        private async Task<ApiResponse<LoginResponse>> SignInAsync(ApiResponse<LoginResponse> response, bool rememberMe)
        {
            if (response.Succeeded && response.Value?.Token is not null)
                await AuthenticationStateProvider.SignInAsync(response.Value.Token.Token, rememberMe);

            return response;
        }
    }
}
