using Vakantiepark.Application.Interfaces;
using Vakantiepark.Shared.DTOs.Accommodation;
using Vakantiepark.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Vakantiepark.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AccommodationController : ControllerBase
    {
        private readonly IAccommodationService AccommodationService;

        public AccommodationController(IAccommodationService accommodationService)
        {
            AccommodationService = accommodationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllAsync()
        {
            List<AccommodationResponse> accommodations = await AccommodationService.GetAllAsync();
            return Ok(ApiResponse<List<AccommodationResponse>>.Ok(accommodations));
        }

        [HttpGet("{code}")]
        public async Task<IActionResult> GetByCodeAsync(string code)
        {
            try
            {
                AccommodationResponse accommodation = await AccommodationService.GetByCodeAsync(code);
                return Ok(ApiResponse<AccommodationResponse>.Ok(accommodation));
            }
            catch (KeyNotFoundException exception)
            {
                return NotFound(ApiResponse<AccommodationResponse>.Fail(exception.Message));
            }
        }
    }
}
