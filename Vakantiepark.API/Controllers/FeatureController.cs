using Vakantiepark.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Vakantiepark.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FeatureController : ApiControllerBase
    {
        private readonly IAccommodationService AccommodationService;

        public FeatureController(IAccommodationService accommodationService)
        {
            AccommodationService = accommodationService;
        }

        // Every feature with its options: what the beheerder's edit form is built from.
        [HttpGet]
        public async Task<IActionResult> GetAllAsync()
            => await RunAsync(() => AccommodationService.GetFeaturesAsync());
    }
}
