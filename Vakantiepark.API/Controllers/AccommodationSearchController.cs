using Microsoft.AspNetCore.Mvc;
using Vakantiepark.Application.Interfaces;
using Vakantiepark.Domain.Filters;

namespace Vakantiepark.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AccommodationSearchController : ApiControllerBase
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
            AccommodationUnitFilter filter = new AccommodationUnitFilter()
            {
                NumberOfGuests = numberOfGuests,
                NumberOfPets = numberOfPets,
                WheelchairAccessible = wheelchairAccessible,
            };
            return await RunAsync(() => SearchService.SearchAsync(
                accommodationTypeSlug, category, arrivalDate, departureDate, filter, zoneId));
        }
    }
}
