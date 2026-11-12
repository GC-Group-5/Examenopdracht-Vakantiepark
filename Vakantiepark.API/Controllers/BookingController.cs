using Vakantiepark.Application.Interfaces;
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
