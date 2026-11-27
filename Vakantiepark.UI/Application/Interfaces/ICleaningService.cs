using Vakantiepark.Shared.DTOs;
using Vakantiepark.Shared.DTOs.Cleaning;

namespace Vakantiepark.UI.Application.Interfaces
{
    // US-32: the cleaner's overview of departures and the buttons to start and finish.
    public interface ICleaningService
    {
        Task<ApiResponse<CleaningDayResponse>> GetDayAsync(DateOnly date);
        Task<ApiResponse<object>> StartAsync(Guid bookingId);
        Task<ApiResponse<object>> FinishAsync(Guid bookingId);
    }
}
