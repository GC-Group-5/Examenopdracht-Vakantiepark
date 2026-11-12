using Microsoft.JSInterop;

namespace Vakantiepark.UI.Authentication
{
    // sessionStorage: the token disappears when the browser closes, which is safer on a shared reception PC.
    public class TokenStorage
    {
        private const string Key = "vakantiepark.token";

        private readonly IJSRuntime JSRuntime;

        public TokenStorage(IJSRuntime jsRuntime)
        {
            JSRuntime = jsRuntime;
        }

        public async Task<string?> GetAsync()
            => await JSRuntime.InvokeAsync<string?>("sessionStorage.getItem", Key);

        public async Task SetAsync(string token)
            => await JSRuntime.InvokeVoidAsync("sessionStorage.setItem", Key, token);

        public async Task RemoveAsync()
            => await JSRuntime.InvokeVoidAsync("sessionStorage.removeItem", Key);
    }
}
