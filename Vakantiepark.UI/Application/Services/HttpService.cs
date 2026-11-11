using Vakantiepark.UI.Application.Interfaces;
using Vakantiepark.Shared.DTOs;
using System.Net.Http.Json;
using System.Text.Json;

namespace Vakantiepark.UI.Application.Services
{
    // Never throws: every call returns an ApiResponse, so pages only have to check Succeeded.
    public class HttpService : IHttpService
    {
        private const string UnexpectedError = "Er is een onverwachte fout opgetreden. Probeer het later opnieuw.";

        private readonly HttpClient HttpClient;
        private readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true
        };

        public HttpService(HttpClient httpClient)
        {
            HttpClient = httpClient;
        }

        public async Task<ApiResponse<TResponse>> GetAsync<TResponse>(string url)
            => await SendAsync<TResponse>(() => HttpClient.GetAsync(url));

        public async Task<ApiResponse<TResponse>> PostAsync<TResponse, TRequest>(string url, TRequest request)
            => await SendAsync<TResponse>(() => HttpClient.PostAsJsonAsync(url, request));

        public async Task<ApiResponse<TResponse>> PutAsync<TResponse, TRequest>(string url, TRequest request)
            => await SendAsync<TResponse>(() => HttpClient.PutAsJsonAsync(url, request));

        public async Task<ApiResponse<TResponse>> DeleteAsync<TResponse>(string url)
            => await SendAsync<TResponse>(() => HttpClient.DeleteAsync(url));

        private async Task<ApiResponse<TResponse>> SendAsync<TResponse>(Func<Task<HttpResponseMessage>> send)
        {
            try
            {
                using HttpResponseMessage response = await send();
                return await response.Content.ReadFromJsonAsync<ApiResponse<TResponse>>(JsonOptions)
                    ?? ApiResponse<TResponse>.Fail(UnexpectedError);
            }
            catch (Exception exception) when (exception is HttpRequestException or JsonException or NotSupportedException or TaskCanceledException)
            {
                return ApiResponse<TResponse>.Fail(UnexpectedError);
            }
        }
    }
}
