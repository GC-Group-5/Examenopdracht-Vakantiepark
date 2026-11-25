using Vakantiepark.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Vakantiepark.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ParkFacilityController : ApiControllerBase
    {
        private readonly IParkFacilityService ParkFacilityService;

        public ParkFacilityController(IParkFacilityService parkFacilityService)
        {
            ParkFacilityService = parkFacilityService;
        }

        // Public: park information holds no personal data (US-20).
        [HttpGet]
        public async Task<IActionResult> GetAllAsync()
            => await RunAsync(() => ParkFacilityService.GetAllAsync());
    }
}
