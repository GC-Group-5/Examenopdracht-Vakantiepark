using System.Net;

namespace Vakantiepark.Application.Helpers
{
    // Reads an email template (embedded resource in Templates/Email) and fills in its {{Placeholders}}.
    public static class EmailTemplate
    {
        public const string BookingConfirmation = "BookingConfirmation";

        private const string ResourcePrefix = "Vakantiepark.Application.Templates.Email.";

        public static RenderedEmail Render(string templateName, IReadOnlyDictionary<string, string> values)
            => new RenderedEmail(
                Fill(Read(templateName + ".html"), values, htmlEncode: true),
                Fill(Read(templateName + ".txt"), values, htmlEncode: false));

        private static string Read(string fileName)
        {
            using Stream stream = typeof(EmailTemplate).Assembly.GetManifestResourceStream(ResourcePrefix + fileName)
                ?? throw new InvalidOperationException($"The email template '{fileName}' was not found.");
            using StreamReader reader = new StreamReader(stream);

            return reader.ReadToEnd();
        }

        // Values typed by a guest are encoded in the HTML version, so a name like "<b>" stays plain text.
        private static string Fill(string template, IReadOnlyDictionary<string, string> values, bool htmlEncode)
        {
            foreach (KeyValuePair<string, string> value in values)
                template = template.Replace("{{" + value.Key + "}}", htmlEncode ? WebUtility.HtmlEncode(value.Value) : value.Value);

            return template;
        }
    }
}
