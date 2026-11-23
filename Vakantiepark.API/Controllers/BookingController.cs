using Vakantiepark.Application.Interfaces;
using Vakantiepark.Domain.Filters;
using Vakantiepark.Shared.DTOs.Booking;
using Vakantiepark.Shared.DTOs;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vakantiepark.Shared.Constants;

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
            [FromQuery] int numberOfChildrenUnder12 = 0,
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
                BookingQuoteResponse quote = await BookingService.GetQuoteAsync(accommodationTypeSlug, arrivalDate, departureDate, filter, numberOfChildrenUnder12, preferredZone);
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

        [HttpGet("mine/{token}")]
        public async Task<IActionResult> GetMineByTokenAsync(string token)
        {
            try
            {
                MyBookingResponse booking = await BookingService.GetMineByTokenAsync(token);
                return Ok(ApiResponse<MyBookingResponse>.Ok(booking));
            }
            catch (ArgumentException exception)
            {
                return BadRequest(ApiResponse<MyBookingResponse>.Fail(exception.Message));
            }
        }

        // The guest logs in with booking number and email via /Auth/Guest; the booking id comes from
        // that token, so a guest can never ask for someone else's booking.
        [HttpGet("mine")]
        [Authorize(Roles = Roles.Gast)]
        public async Task<IActionResult> GetMineAsync()
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimNames.BookingId), out Guid bookingId))
                return Unauthorized(ApiResponse<MyBookingResponse>.Fail());

            try
            {
                MyBookingResponse booking = await BookingService.GetMineAsync(bookingId);
                return Ok(ApiResponse<MyBookingResponse>.Ok(booking));
            }
            catch (ArgumentException exception)
            {
                return NotFound(ApiResponse<MyBookingResponse>.Fail(exception.Message));
            }
        }

        [HttpPost("mine/cancel")]
        public async Task<IActionResult> CancelAsync(BookingAccessRequest request)
        {
            try
            {
                MyBookingResponse booking = await BookingService.CancelAsync(request, GuestBookingId());
                return Ok(ApiResponse<MyBookingResponse>.Ok(booking));
            }
            catch (ArgumentException exception)
            {
                return BadRequest(ApiResponse<MyBookingResponse>.Fail(exception.Message));
            }
        }

        [HttpPost("mine/cancel/quote")]
        public async Task<IActionResult> GetCancellationQuoteAsync(BookingAccessRequest request)
        {
            try
            {
                CancellationQuoteResponse quote = await BookingService.GetCancellationQuoteAsync(request, GuestBookingId());
                return Ok(ApiResponse<CancellationQuoteResponse>.Ok(quote));
            }
            catch (ArgumentException exception)
            {
                return BadRequest(ApiResponse<CancellationQuoteResponse>.Fail(exception.Message));
            }
        }

        [HttpPost("mine/change")]
        public async Task<IActionResult> ChangeAsync(ChangeBookingRequest request)
        {
            try
            {
                MyBookingResponse booking = await BookingService.ChangeAsync(request, GuestBookingId());
                return Ok(ApiResponse<MyBookingResponse>.Ok(booking));
            }
            catch (ArgumentException exception)
            {
                return BadRequest(ApiResponse<MyBookingResponse>.Fail(exception.Message));
            }
        }

        [HttpPost("mine/change/quote")]
        public async Task<IActionResult> GetChangeQuoteAsync(ChangeBookingRequest request)
        {
            try
            {
                ChangeQuoteResponse quote = await BookingService.GetChangeQuoteAsync(request, GuestBookingId());
                return Ok(ApiResponse<ChangeQuoteResponse>.Ok(quote));
            }
            catch (ArgumentException exception)
            {
                return BadRequest(ApiResponse<ChangeQuoteResponse>.Fail(exception.Message));
            }
        }

        // US-12: a guest logged in via /Auth/Guest changes or cancels the booking in his token;
        // otherwise the request must carry the code from the email link.
        private Guid? GuestBookingId()
            => User.IsInRole(Roles.Gast) && Guid.TryParse(User.FindFirstValue(ClaimNames.BookingId), out Guid bookingId)
                ? bookingId
                : null;
    }
}
