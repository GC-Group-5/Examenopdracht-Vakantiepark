using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vakantiepark.API.Extensions;
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

        [HttpPost("{bookingId:guid}/Start")]
        public async Task<IActionResult> StartAsync(Guid bookingId)
            => await RunAsync(() => CleaningService.StartAsync(bookingId, GetCleanerId()), "Schoonmaak gestart.");

        [HttpPost("{bookingId:guid}/Finish")]
        public async Task<IActionResult> FinishAsync(Guid bookingId)
            => await RunAsync(() => CleaningService.FinishAsync(bookingId, GetCleanerId()), "Huisje is schoon.");

        // The cleaner comes from the token, so nobody can mark a unit in someone else's name.
        private Guid GetCleanerId()
            => User.GetUserId() ?? throw new UnauthorizedAccessException("Log opnieuw in.");
    }
}
