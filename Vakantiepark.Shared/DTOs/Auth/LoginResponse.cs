namespace Vakantiepark.Shared.DTOs.Auth
{
    // Either a token (logged in) or RequiresCode (a login code was emailed, see US-44).
    public class LoginResponse
    {
        public bool RequiresCode { get; set; }
        public TokenResponse? Token { get; set; }

        // Only after a login code with "Onthoud dit apparaat": the browser keeps it and sends it with the next login.
        public string? DeviceToken { get; set; }
    }
}
