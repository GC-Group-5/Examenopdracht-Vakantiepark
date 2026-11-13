namespace Vakantiepark.Infrastructure.Settings
{
    // Host and Port come from the "mailpit" connection string that Vakantiepark.AppHost supplies.
    public class EmailSettings
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
        public string FromAddress { get; set; } = string.Empty;
    }
}
