using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vakantiepark.Application.Interfaces;
using Vakantiepark.Shared.Constants;

namespace Vakantiepark.API.Controllers
{
    // US-32: the cleaner's overview. The response holds no personal data of guests.
    [ApiController]
    [Authorize(Roles = Roles.Schoonmaker)]
    [Route("[controller]")]
    public class CleaningController : ApiControllerBase
    {
        private readonly ICleaningService CleaningService;

        public CleaningController(ICleaningService cleaningService)
        {
            CleaningService = cleaningService;
        }

        // Without ?date= the overview shows today.
        [HttpGet]
        public async Task<IActionResult> GetDayAsync([FromQuery] DateOnly? date)
            => await RunAsync(() => CleaningService.GetDayAsync(date));
    }
}
