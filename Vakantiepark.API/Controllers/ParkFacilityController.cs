using Vakantiepark.Application.Interfaces;
using Vakantiepark.Shared.DTOs.Park;
using Vakantiepark.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Vakantiepark.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ParkFacilityController : ControllerBase
    {
        private readonly IParkFacilityService ParkFacilityService;

        public ParkFacilityController(IParkFacilityService parkFacilityService)
        {
            ParkFacilityService = parkFacilityService;
        }

        // Public: park information holds no personal data (US-20).
        [HttpGet]
        public async Task<IActionResult> GetAllAsync()
        {
            List<ParkFacilityResponse> facilities = await ParkFacilityService.GetAllAsync();
            return Ok(ApiResponse<List<ParkFacilityResponse>>.Ok(facilities));
        }
    }
}
