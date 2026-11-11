namespace Vakantiepark.Application.Settings
{
    // Bound from the "JWT" configuration section. The Key is supplied by Vakantiepark.AppHost, never by appsettings.json.
    public class JwtSettings
    {
        public const int MinimumKeyLength = 32;

        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public int ExpiryMinutes { get; set; }
    }
}
