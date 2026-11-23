using Vakantiepark.Shared.DTOs.Park;

namespace Vakantiepark.Application.Interfaces
{
    public interface IParkFacilityService
    {
        Task<List<ParkFacilityResponse>> GetAllAsync();
    }
}
