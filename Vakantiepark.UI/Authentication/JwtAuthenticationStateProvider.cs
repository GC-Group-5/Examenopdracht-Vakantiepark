using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using System.Text.Json;

namespace Vakantiepark.UI.Authentication
{
    public class JwtAuthenticationStateProvider : AuthenticationStateProvider
    {
        private static readonly AuthenticationState Anonymous = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

        private readonly TokenStorage TokenStorage;

        public JwtAuthenticationStateProvider(TokenStorage tokenStorage)
        {
            TokenStorage = tokenStorage;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            string? token = await TokenStorage.GetAsync();
            if (string.IsNullOrWhiteSpace(token))
                return Anonymous;

            try
            {
                List<Claim> claims = JwtParser.ReadClaims(token);
                if (JwtParser.IsExpired(claims))
                {
                    await TokenStorage.RemoveAsync();
                    return Anonymous;
                }

                ClaimsIdentity identity = new ClaimsIdentity(claims, "jwt", ClaimTypes.Name, ClaimTypes.Role);
                return new AuthenticationState(new ClaimsPrincipal(identity));
            }
            catch (Exception exception) when (exception is FormatException or JsonException)
            {
                await TokenStorage.RemoveAsync();
                return Anonymous;
            }
        }

        public async Task SignInAsync(string token)
        {
            await TokenStorage.SetAsync(token);
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        public async Task SignOutAsync()
        {
            await TokenStorage.RemoveAsync();
            NotifyAuthenticationStateChanged(Task.FromResult(Anonymous));
        }
    }
}
