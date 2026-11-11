using System.Net.Http.Headers;

namespace Vakantiepark.UI.Authentication
{
    // Adds the token of the logged-in user to every request to the API.
    public class AuthTokenHandler : DelegatingHandler
    {
        private readonly TokenStorage TokenStorage;

        public AuthTokenHandler(TokenStorage tokenStorage)
        {
            TokenStorage = tokenStorage;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string? token = await TokenStorage.GetAsync();
            if (!string.IsNullOrWhiteSpace(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
