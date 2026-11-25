using Vakantiepark.Shared.DTOs.Occupancy;

namespace Vakantiepark.Application.Interfaces
{
    public interface IOccupancyService
    {
        Task<OccupancyResponse> GetAsync(OccupancyRequest request);
    }
}
