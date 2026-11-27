using Microsoft.JSInterop;
using System.Security.Claims;
using Vakantiepark.Shared.Constants;

namespace Vakantiepark.UI.Authentication
{
    // Guests use localStorage so they stay logged in on their own phone during the stay (the token expires after departure).
    // Staff use sessionStorage: the token disappears when the browser closes, which is safer on a shared reception PC,
    // unless they tick "Onthoud mij" on their own device.
    public class TokenStorage
    {
        private const string Key = "vakantiepark.token";

        private readonly IJSRuntime JSRuntime;

        public TokenStorage(IJSRuntime jsRuntime)
        {
            JSRuntime = jsRuntime;
        }

        public async Task<string?> GetAsync()
        {
            string? token = await JSRuntime.InvokeAsync<string?>("sessionStorage.getItem", Key);
            if (token is null)
                token = await JSRuntime.InvokeAsync<string?>("localStorage.getItem", Key);

            return token;
        }

        public async Task SetAsync(string token, bool rememberMe)
        {
            await RemoveAsync();

            bool isGuest = JwtParser.ReadClaims(token).Any(claim => claim.Type == ClaimTypes.Role && claim.Value == Roles.Gast);
            if (isGuest || rememberMe)
                await JSRuntime.InvokeVoidAsync("localStorage.setItem", Key, token);
            else
                await JSRuntime.InvokeVoidAsync("sessionStorage.setItem", Key, token);
        }

        public async Task RemoveAsync()
        {
            await JSRuntime.InvokeVoidAsync("sessionStorage.removeItem", Key);
            await JSRuntime.InvokeVoidAsync("localStorage.removeItem", Key);
        }
    }
}
