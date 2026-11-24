using Vakantiepark.Shared.DTOs;
using Vakantiepark.Shared.DTOs.Park;
using Vakantiepark.UI.Application.Interfaces;

namespace Vakantiepark.UI.Application.Services
{
    public class ParkFacilityService : IParkFacilityService
    {
        private readonly IHttpService HttpService;

        public ParkFacilityService(IHttpService httpService)
        {
            HttpService = httpService;
        }

        public async Task<ApiResponse<List<ParkFacilityResponse>>> GetAllAsync()
            => await HttpService.GetAsync<List<ParkFacilityResponse>>("/ParkFacility");
    }
}
