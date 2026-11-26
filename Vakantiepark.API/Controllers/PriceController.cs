using Microsoft.AspNetCore.Mvc;
using Vakantiepark.Application.Interfaces;

namespace Vakantiepark.API.Controllers
{
    // US-07: the same calculation the booking flow uses, exposed for the
    // website's prijs specificatie. The server always recalculates; a price
    // coming from the browser is never trusted.
    
    [ApiController]
    [Route("[controller]")]
    public class PriceController : ApiControllerBase
    {
        private readonly IPriceService PriceService;

        public PriceController(IPriceService priceService)
        {
            PriceService = priceService;
        }

        [HttpGet]
        public async Task<IActionResult> CalculateAsync(
            [FromQuery] string accommodationTypeSlug,
            [FromQuery] DateOnly arrivalDate,
            [FromQuery] DateOnly departureDate,
            [FromQuery] int numberOfGuests,
            [FromQuery] int numberOfChildrenUnder12 = 0,
            [FromQuery] int numberOfPets = 0)
            => await RunAsync(() => PriceService.CalculateAsync(
                accommodationTypeSlug, arrivalDate, departureDate, numberOfGuests, numberOfChildrenUnder12, numberOfPets));
    }
}
