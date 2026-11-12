using Vakantiepark.Application.Interfaces;
using Vakantiepark.Shared.DTOs.Accommodation;
using Vakantiepark.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Vakantiepark.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FeatureController : ControllerBase
    {
        private readonly IAccommodationService AccommodationService;

        public FeatureController(IAccommodationService accommodationService)
        {
            AccommodationService = accommodationService;
        }

        // Every feature with its options: what the beheerder's edit form is built from.
        [HttpGet]
        public async Task<IActionResult> GetAllAsync()
        {
            List<FeatureResponse> features = await AccommodationService.GetFeaturesAsync();
            return Ok(ApiResponse<List<FeatureResponse>>.Ok(features));
        }
    }
}
