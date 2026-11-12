using Microsoft.Extensions.Logging;

namespace Vakantiepark.Domain.Interfaces.Helpers
{
    public interface IEmailSender
    {
        Task SendAsync(string toAddress, string subject, string htmlBody, string textBody);
    }
}
