using Microsoft.AspNetCore.Mvc;
using Vakantiepark.Application.Interfaces;
using Vakantiepark.Shared.DTOs.Accommodation;
using Vakantiepark.Shared.DTOs;

namespace Vakantiepark.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AccommodationTypeController : ControllerBase
    {
        private readonly IAccommodationService AccommodationService;

        public AccommodationTypeController(IAccommodationService accommodationService)
        {
            AccommodationService = accommodationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllAsync()
        {
            List<AccommodationTypeResponse> accommodationTypes = await AccommodationService.GetTypesAsync();
            return Ok(ApiResponse<List<AccommodationTypeResponse>>.Ok(accommodationTypes));
        }

        [HttpGet("{slug}")]
        public async Task<IActionResult> GetBySlugAsync(string slug)
        {
            try
            {
                AccommodationTypeResponse accommodationType = await AccommodationService.GetTypeBySlugAsync(slug);
                return Ok(ApiResponse<AccommodationTypeResponse>.Ok(accommodationType));
            }
            catch (KeyNotFoundException exception)
            {
                return NotFound(ApiResponse<AccommodationTypeResponse>.Fail(exception.Message));
            }
        }
    }
}
