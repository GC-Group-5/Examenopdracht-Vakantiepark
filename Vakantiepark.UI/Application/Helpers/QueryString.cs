using System.Globalization;

namespace Vakantiepark.UI.Application.Helpers
{
    // Builds "path?name=value&…" for API calls and page links. Empty values are left out and the
    // rest is escaped, so a search field with "&" or a space can never break the address.
    public class QueryString
    {
        private readonly List<string> Parameters = [];

        public QueryString Add(string name, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                Parameters.Add($"{name}={Uri.EscapeDataString(value.Trim())}");

            return this;
        }

        public QueryString Add(string name, DateOnly? value)
            => Add(name, value?.ToString(DateFormats.Query, CultureInfo.InvariantCulture));

        public QueryString Add(string name, int? value)
            => Add(name, value?.ToString(CultureInfo.InvariantCulture));

        public QueryString Add(string name, Guid? value)
            => Add(name, value?.ToString());

        // The API and the pages read a missing flag as false, so only true is sent.
        public QueryString Add(string name, bool value)
            => value ? Add(name, "true") : this;

        public string ToUrl(string path)
            => Parameters.Count == 0 ? path : $"{path}?{string.Join("&", Parameters)}";
    }
}
