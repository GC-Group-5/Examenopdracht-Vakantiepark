using Vakantiepark.Application.Interfaces;
using Vakantiepark.Domain.Filters;
using Vakantiepark.Shared.DTOs.Booking;
using Vakantiepark.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Vakantiepark.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class BookingController : ControllerBase
    {
        private readonly IBookingService BookingService;

        public BookingController(IBookingService bookingService)
        {
            BookingService = bookingService;
        }

        // The summary before confirming: checks the stay and returns price, times and terms.
        [HttpGet("quote")]
        public async Task<IActionResult> GetQuoteAsync(
            [FromQuery] string accommodationTypeSlug,
            [FromQuery] DateOnly arrivalDate,
            [FromQuery] DateOnly departureDate,
            [FromQuery] int numberOfGuests,
            [FromQuery] string? preferredZone,
            [FromQuery] int numberOfPets = 0,
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
                BookingQuoteResponse quote = await BookingService.GetQuoteAsync(accommodationTypeSlug, arrivalDate, departureDate, filter, preferredZone);
                return Ok(ApiResponse<BookingQuoteResponse>.Ok(quote));
            }
            catch (ArgumentException exception)
            {
                return BadRequest(ApiResponse<BookingQuoteResponse>.Fail(exception.Message));
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateAsync(CreateBookingRequest request)
        {
            try
            {
                BookingResponse booking = await BookingService.CreateAsync(request);
                return Ok(ApiResponse<BookingResponse>.Ok(booking));
            }
            catch (ArgumentException exception)
            {
                return BadRequest(ApiResponse<BookingResponse>.Fail(exception.Message));
            }
        }
    }
}
