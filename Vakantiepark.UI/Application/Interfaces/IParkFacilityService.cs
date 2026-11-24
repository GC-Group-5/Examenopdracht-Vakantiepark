using Vakantiepark.Shared.DTOs;
using Vakantiepark.Shared.DTOs.Park;

namespace Vakantiepark.UI.Application.Interfaces
{
    // Park information for guests in the PWA (US-20).
    public interface IParkFacilityService
    {
        Task<ApiResponse<List<ParkFacilityResponse>>> GetAllAsync();
    }
}
