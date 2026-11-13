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

        // The stay and park rules the booking form shows in its summary, e.g. GET /Booking/info?type=berk-4.
        [HttpGet("info")]
        public async Task<IActionResult> GetInfoAsync([FromQuery] string type)
        {
            try
            {
                BookingInfoResponse info = await BookingService.GetInfoAsync(type);
                return Ok(ApiResponse<BookingInfoResponse>.Ok(info));
            }
            catch (KeyNotFoundException exception)
            {
                return NotFound(ApiResponse<BookingInfoResponse>.Fail(exception.Message));
            }
        }
    }
}
