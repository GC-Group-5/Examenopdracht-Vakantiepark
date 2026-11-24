using Vakantiepark.Shared.DTOs;
using Vakantiepark.Shared.DTOs.Occupancy;

namespace Vakantiepark.UI.Application.Interfaces
{
    // Staff screen with the status of every unit on one date (US-16).
    public interface IOccupancyService
    {
        Task<ApiResponse<OccupancyResponse>> GetAsync(OccupancyRequest request);
    }
}
