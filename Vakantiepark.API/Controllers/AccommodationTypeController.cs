using Microsoft.AspNetCore.Mvc;
using Vakantiepark.Application.Interfaces;

namespace Vakantiepark.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AccommodationTypeController : ApiControllerBase
    {
        private readonly IAccommodationService AccommodationService;

        public AccommodationTypeController(IAccommodationService accommodationService)
        {
            AccommodationService = accommodationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllAsync()
            => await RunAsync(() => AccommodationService.GetTypesAsync());

        [HttpGet("{slug}")]
        public async Task<IActionResult> GetBySlugAsync(string slug)
            => await RunAsync(() => AccommodationService.GetTypeBySlugAsync(slug));
    }
}
