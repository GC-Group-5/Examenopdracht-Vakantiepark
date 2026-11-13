using Vakantiepark.Domain.Interfaces.Helpers;
using Vakantiepark.Infrastructure.Settings;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Vakantiepark.Infrastructure.Helpers
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly EmailSettings Settings;

        public SmtpEmailSender(EmailSettings settings)
        {
            Settings = settings;
        }

        public async Task SendAsync(string toAddress, string subject, string htmlBody, string textBody)
        {
            MimeMessage message = new MimeMessage();
            message.From.Add(new MailboxAddress("Vakantiepark D'r op Uut", Settings.FromAddress));
            message.To.Add(MailboxAddress.Parse(toAddress));
            message.Subject = subject;
            message.Body = new BodyBuilder()
            {
                HtmlBody = htmlBody,
                TextBody = textBody
            }.ToMessageBody();

            using SmtpClient client = new SmtpClient();
            // Mailpit runs locally without TLS or login; relaying to a real inbox is Mailpit's job (see AppHost).
            await client.ConnectAsync(Settings.Host, Settings.Port, SecureSocketOptions.None);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}
