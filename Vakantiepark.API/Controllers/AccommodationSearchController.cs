using Microsoft.AspNetCore.Mvc;
using Vakantiepark.Application.Interfaces;
using Vakantiepark.Shared.DTOs;
using Vakantiepark.Shared.DTOs.Accommodation;

namespace Vakantiepark.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AccommodationSearchController : ControllerBase
    {
        private readonly IAccommodationSearchService SearchService;

        public AccommodationSearchController(IAccommodationSearchService searchService)
        {
            SearchService = searchService;
        }

        [HttpGet]
        public async Task<IActionResult> SearchAsync(
            [FromQuery] string? accommodationTypeSlug,
            [FromQuery] DateOnly arrivalDate,
            [FromQuery] DateOnly departureDate,
            [FromQuery] int numberOfGuests,
            [FromQuery] int numberOfPets = 0,
            [FromQuery] Guid? zoneId = null)
        {
            try
            {
                AccommodationSearchResponse result = await SearchService.SearchAsync(
                    accommodationTypeSlug, arrivalDate, departureDate, numberOfGuests, numberOfPets, zoneId);

                return Ok(ApiResponse<AccommodationSearchResponse>.Ok(result));
            }
            catch (ArgumentException exception)
            {
                return BadRequest(ApiResponse<AccommodationSearchResponse>.Fail(exception.Message));
            }
        }
    }
}
