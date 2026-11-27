using Microsoft.JSInterop;

namespace Vakantiepark.UI.Authentication
{
    // US-44: remembers this browser per staff member after "Onthoud dit apparaat", so the login code is skipped for 30 days.
    // Kept in localStorage (unlike the login token) because it must survive closing the browser.
    public class TrustedDeviceStorage
    {
        private const string KeyPrefix = "vakantiepark.device.";

        private readonly IJSRuntime JSRuntime;

        public TrustedDeviceStorage(IJSRuntime jsRuntime)
        {
            JSRuntime = jsRuntime;
        }

        public async Task<string?> GetAsync(string email)
            => await JSRuntime.InvokeAsync<string?>("localStorage.getItem", Key(email));

        public async Task SetAsync(string email, string deviceToken)
            => await JSRuntime.InvokeVoidAsync("localStorage.setItem", Key(email), deviceToken);

        // One key per email, so several colleagues can each remember the same reception PC.
        private static string Key(string email)
            => KeyPrefix + email.Trim().ToLowerInvariant();
    }
}
