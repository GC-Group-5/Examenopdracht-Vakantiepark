using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vakantiepark.Application.Interfaces;
using Vakantiepark.Shared.Constants;
using Vakantiepark.Shared.DTOs;
using Vakantiepark.Shared.DTOs.Occupancy;

namespace Vakantiepark.API.Controllers
{
    // Staff endpoint (US-16): the occupancy of the park shows bookings, so only medewerkers and beheerders.
    [ApiController]
    [Authorize(Roles = Roles.Staff)]
    [Route("[controller]")]
    public class OccupancyController : ControllerBase
    {
        private readonly IOccupancyService OccupancyService;

        public OccupancyController(IOccupancyService occupancyService)
        {
            OccupancyService = occupancyService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAsync([FromQuery] OccupancyRequest request)
        {
            try
            {
                OccupancyResponse occupancy = await OccupancyService.GetAsync(request);
                return Ok(ApiResponse<OccupancyResponse>.Ok(occupancy));
            }
            catch (ArgumentException exception)
            {
                return BadRequest(ApiResponse<OccupancyResponse>.Fail(exception.Message));
            }
        }
    }
}
