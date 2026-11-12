namespace Vakantiepark.Infrastructure.Settings
{
    // By default Host and Port come from the "mailpit" connection string that Vakantiepark.AppHost supplies.
    // A developer can point to an external test SMTP server (e.g. Mailtrap) with "Email:Smtp" in the API's user-secrets.
    public class EmailSettings
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
        public string FromAddress { get; set; } = string.Empty;

        // Empty for Mailpit, which needs no login and no TLS.
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool UseTls { get; set; }
    }
}
