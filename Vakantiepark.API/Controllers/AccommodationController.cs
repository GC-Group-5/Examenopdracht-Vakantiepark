using Vakantiepark.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Vakantiepark.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AccommodationController : ApiControllerBase
    {
        private readonly IAccommodationService AccommodationService;

        public AccommodationController(IAccommodationService accommodationService)
        {
            AccommodationService = accommodationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllAsync()
            => await RunAsync(() => AccommodationService.GetAllAsync());

        [HttpGet("zones")]
        public async Task<IActionResult> GetZoneOptionsAsync()
            => await RunAsync(() => AccommodationService.GetZoneOptionsAsync());

        [HttpGet("{code}")]
        public async Task<IActionResult> GetByCodeAsync(string code)
            => await RunAsync(() => AccommodationService.GetByCodeAsync(code));
    }
}
