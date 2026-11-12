using Microsoft.AspNetCore.Mvc;
using Vakantiepark.Application.Interfaces;
using Vakantiepark.Shared.DTOs;
using Vakantiepark.Shared.DTOs.Price;

namespace Vakantiepark.API.Controllers
{
    // US-07: the same calculation the booking flow uses, exposed for the
    // website's prijs specificatie. The server always recalculates; a price
    // coming from the browser is never trusted.
    
    [ApiController]
    [Route("[controller]")]
    public class PriceController : ControllerBase
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
            [FromQuery] int numberOfGuests)
        {
            try
            {
                PriceSpecificationResponse price = await PriceService.CalculateAsync(
                    accommodationTypeSlug, arrivalDate, departureDate, numberOfGuests);

                return Ok(ApiResponse<PriceSpecificationResponse>.Ok(price));
            }
            catch (ArgumentException exception)
            {
                return BadRequest(ApiResponse<PriceSpecificationResponse>.Fail(exception.Message));
            }
            catch (InvalidOperationException exception)
            {
                return BadRequest(ApiResponse<PriceSpecificationResponse>.Fail(exception.Message));
            }
        }
    }
}
