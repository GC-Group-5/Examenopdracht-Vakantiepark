using Microsoft.AspNetCore.Mvc;
using Vakantiepark.Application.Interfaces;
using Vakantiepark.Domain.Filters;
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
            [FromQuery] Guid? zoneId = null,
            [FromQuery] string? category = null,
            [FromQuery] bool wheelchairAccessible = false)
        {
            try
            {
                AccommodationUnitFilter filter = new AccommodationUnitFilter()
                {
                    NumberOfGuests = numberOfGuests,
                    NumberOfPets = numberOfPets,
                    WheelchairAccessible = wheelchairAccessible,
                };
                AccommodationSearchResponse result = await SearchService.SearchAsync(
                    accommodationTypeSlug, category, arrivalDate, departureDate, filter, zoneId);

                return Ok(ApiResponse<AccommodationSearchResponse>.Ok(result));
            }
            catch (ArgumentException exception)
            {
                return BadRequest(ApiResponse<AccommodationSearchResponse>.Fail(exception.Message));
            }
        }
    }
}
