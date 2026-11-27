using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vakantiepark.Application.Interfaces;
using Vakantiepark.Shared.Constants;
using Vakantiepark.Shared.DTOs;
using Vakantiepark.Shared.DTOs.Swimming;

namespace Vakantiepark.API.Controllers
{
    // US-22: only guests logged in with booking number and email; the booking comes from the token,
    // so a guest only ever sees the days of his own stay.
    [ApiController]
    [Route("[controller]")]
    [Authorize(Roles = Roles.Gast)]
    public class SwimSlotController : ApiControllerBase
    {
        private readonly ISwimSlotService SwimSlotService;

        public SwimSlotController(ISwimSlotService swimSlotService)
        {
            SwimSlotService = swimSlotService;
        }

        [HttpGet("Days")]
        public async Task<IActionResult> GetDaysAsync()
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimNames.BookingId), out Guid bookingId))
                return Unauthorized(ApiResponse<List<DateOnly>>.Fail());

            return await RunAsync(() => SwimSlotService.GetDaysAsync(bookingId));
        }

        [HttpGet]
        public async Task<IActionResult> GetByDateAsync([FromQuery] DateOnly date)
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimNames.BookingId), out Guid bookingId))
                return Unauthorized(ApiResponse<List<SwimSlotResponse>>.Fail());

            return await RunAsync(() => SwimSlotService.GetByDateAsync(bookingId, date));
        }
    }
}
