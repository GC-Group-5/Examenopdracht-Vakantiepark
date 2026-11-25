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
    public class BookingController : ApiControllerBase
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
            AccommodationUnitFilter filter = new AccommodationUnitFilter()
            {
                NumberOfGuests = numberOfGuests,
                NumberOfPets = numberOfPets,
                WheelchairAccessible = wheelchairAccessible,
            };

            return await RunAsync(() => BookingService.GetQuoteAsync(accommodationTypeSlug, arrivalDate, departureDate, filter, numberOfChildrenUnder12, preferredZone));
        }

        [HttpPost]
        public async Task<IActionResult> CreateAsync(CreateBookingRequest request)
            => await RunAsync(() => BookingService.CreateAsync(request));

        [HttpGet("mine/{token}")]
        public async Task<IActionResult> GetMineByTokenAsync(string token)
            => await RunAsync(() => BookingService.GetMineByTokenAsync(token));

        // The guest logs in with booking number and email via /Auth/Guest; the booking id comes from
        // that token, so a guest can never ask for someone else's booking.
        [HttpGet("mine")]
        [Authorize(Roles = Roles.Gast)]
        public async Task<IActionResult> GetMineAsync()
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimNames.BookingId), out Guid bookingId))
                return Unauthorized(ApiResponse<MyBookingResponse>.Fail());

            return await RunAsync(() => BookingService.GetMineAsync(bookingId));
        }

        [HttpPost("mine/cancel")]
        public async Task<IActionResult> CancelAsync(BookingAccessRequest request)
            => await RunAsync(() => BookingService.CancelAsync(request, GuestBookingId()));

        [HttpPost("mine/cancel/quote")]
        public async Task<IActionResult> GetCancellationQuoteAsync(BookingAccessRequest request)
            => await RunAsync(() => BookingService.GetCancellationQuoteAsync(request, GuestBookingId()));

        [HttpPost("mine/change")]
        public async Task<IActionResult> ChangeAsync(ChangeBookingRequest request)
            => await RunAsync(() => BookingService.ChangeAsync(request, GuestBookingId()));

        [HttpPost("mine/change/quote")]
        public async Task<IActionResult> GetChangeQuoteAsync(ChangeBookingRequest request)
            => await RunAsync(() => BookingService.GetChangeQuoteAsync(request, GuestBookingId()));

        // US-12: a guest logged in via /Auth/Guest changes or cancels the booking in his token;
        // otherwise the request must carry the code from the email link.
        private Guid? GuestBookingId()
            => User.IsInRole(Roles.Gast) && Guid.TryParse(User.FindFirstValue(ClaimNames.BookingId), out Guid bookingId)
                ? bookingId
                : null;
    }
}
