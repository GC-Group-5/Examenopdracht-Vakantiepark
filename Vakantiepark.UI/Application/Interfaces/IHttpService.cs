using Vakantiepark.Shared.DTOs;

namespace Vakantiepark.UI.Application.Interfaces
{
    public interface IHttpService
    {
        Task<ApiResponse<TResponse>> GetAsync<TResponse>(string url);
        Task<ApiResponse<TResponse>> PostAsync<TResponse, TRequest>(string url, TRequest request);
        Task<ApiResponse<TResponse>> PutAsync<TResponse, TRequest>(string url, TRequest request);
        Task<ApiResponse<TResponse>> DeleteAsync<TResponse>(string url);
    }
}
