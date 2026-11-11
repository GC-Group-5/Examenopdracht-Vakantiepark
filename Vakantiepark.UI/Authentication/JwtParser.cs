using System.Security.Claims;
using System.Text.Json;

namespace Vakantiepark.UI.Authentication
{
    // Reads the claims from a token so the UI knows who is logged in. The API still validates every token itself.
    public static class JwtParser
    {
        public static List<Claim> ReadClaims(string token)
        {
            string[] parts = token.Split('.');
            if (parts.Length != 3)
                throw new FormatException("The token does not have three parts.");

            Dictionary<string, JsonElement> payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(DecodeBase64Url(parts[1]))
                ?? throw new FormatException("The token payload is empty.");

            List<Claim> claims = [];
            foreach ((string type, JsonElement value) in payload)
            {
                if (value.ValueKind == JsonValueKind.Array)
                    claims.AddRange(value.EnumerateArray().Select(item => new Claim(type, item.ToString())));
                else
                    claims.Add(new Claim(type, value.ToString()));
            }

            return claims;
        }

        public static bool IsExpired(List<Claim> claims)
        {
            string? expires = claims.FirstOrDefault(x => x.Type == "exp")?.Value;
            if (!long.TryParse(expires, out long seconds))
                return true;

            return DateTimeOffset.FromUnixTimeSeconds(seconds) <= DateTimeOffset.UtcNow;
        }

        private static byte[] DecodeBase64Url(string value)
        {
            string base64 = value.Replace('-', '+').Replace('_', '/');
            base64 = base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=');

            return Convert.FromBase64String(base64);
        }
    }
}
