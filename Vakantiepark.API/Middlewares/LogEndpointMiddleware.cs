namespace Vakantiepark.API.Middlewares
{
    // Development only: logs every request with its status code, and warns when the casing of a route
    // segment differs from the controller name (routes are PascalCase, see AGENTS.md).
    public class LogEndpointMiddleware
    {
        private readonly RequestDelegate Next;
        private readonly ILogger<LogEndpointMiddleware> Logger;

        public LogEndpointMiddleware(RequestDelegate next, ILogger<LogEndpointMiddleware> logger)
        {
            Next = next;
            Logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            await Next(context);

            if (context.GetEndpoint() is not RouteEndpoint endpoint)
                return;

            string method = context.Request.Method;
            string path = context.Request.Path.Value ?? "/";
            int statusCode = context.Response.StatusCode;
            bool succeeded = statusCode is >= 200 and < 300;

            if (method == HttpMethods.Options && succeeded)
                return;

            string? casingWarning = GetCasingWarning(endpoint.RoutePattern.RawText);
            if (casingWarning is not null)
                Logger.LogWarning("Endpoint {Method} {Path}: {Warning}", method, path, casingWarning);

            if (succeeded)
                Logger.LogInformation("Endpoint {Method} {Path} | {StatusCode} SUCCEEDED", method, path, statusCode);
            else
                Logger.LogWarning("Endpoint {Method} {Path} | {StatusCode} FAILED", method, path, statusCode);
        }

        private static string? GetCasingWarning(string? routePattern)
        {
            List<string> segments = [.. (routePattern ?? string.Empty)
                .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(x => !x.StartsWith('{'))];

            if (segments.Count < 2)
                return null;

            bool controllerIsUpper = char.IsUpper(segments[0][0]);
            List<string> faulty = [.. segments.Skip(1).Where(x => char.IsUpper(x[0]) != controllerIsUpper)];
            if (faulty.Count == 0)
                return null;

            return $"segment(s) {string.Join(", ", faulty.Select(x => $"'{x}'"))} do not match the casing of '{segments[0]}'";
        }
    }
}
