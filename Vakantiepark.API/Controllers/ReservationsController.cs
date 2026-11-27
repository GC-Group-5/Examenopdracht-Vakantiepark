using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Vakantiepark.Application.Interfaces;
using Vakantiepark.Shared.Constants;
using Vakantiepark.Shared.DTOs.Reservations;

namespace Vakantiepark.API.Controllers
{
    // Staff endpoints (US-13): only medewerkers and beheerders see other people's bookings.
    [ApiController]
    [Authorize(Roles = Roles.Staff)]
    [Route("[controller]")]
    public class ReservationsController : ApiControllerBase
    {
        private readonly IReservationService ReservationService;
        private readonly IBookingService BookingService;

        public ReservationsController(IReservationService reservationService, IBookingService bookingService)
        {
            ReservationService = reservationService;
            BookingService = bookingService;
        }

        [HttpGet]
        public async Task<IActionResult> SearchAsync([FromQuery] ReservationSearchRequest request)
            => await RunAsync(() => ReservationService.SearchAsync(request));

        // US-15: who arrives and departs on this day.
        [HttpGet("Day")]
        public async Task<IActionResult> GetDayOverviewAsync([FromQuery, BindRequired] DateOnly date)
            => await RunAsync(() => ReservationService.GetDayOverviewAsync(date));

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetByIdAsync(Guid id)
            => await RunAsync(() => ReservationService.GetByIdAsync(id));

        // US-51: the free units for the new dates (or other types when the own type is full), before saving.
        [HttpGet("{id:guid}/ChangeOptions")]
        public async Task<IActionResult> GetChangeOptionsAsync(
            Guid id,
            [FromQuery, BindRequired] DateOnly arrivalDate,
            [FromQuery, BindRequired] DateOnly departureDate,
            [FromQuery, BindRequired] int numberOfGuests)
            => await RunAsync(() => BookingService.GetChangeOptionsAsync(id, arrivalDate, departureDate, numberOfGuests));

        // US-14: the same rules as for a guest, without the term; returns the updated reservation.
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> ChangeAsync(Guid id, StaffChangeBookingRequest request)
            => await RunAsync(async () =>
            {
                await BookingService.ChangeByStaffAsync(id, request, StaffUserId());
                return await ReservationService.GetByIdAsync(id);
            });

        [HttpGet("{id:guid}/Cancel/Quote")]
        public async Task<IActionResult> GetCancellationQuoteAsync(Guid id)
            => await RunAsync(() => BookingService.GetCancellationQuoteForStaffAsync(id));

        [HttpPost("{id:guid}/Cancel")]
        public async Task<IActionResult> CancelAsync(Guid id, StaffCancelBookingRequest request)
            => await RunAsync(async () =>
            {
                await BookingService.CancelByStaffAsync(id, request, StaffUserId());
                return await ReservationService.GetByIdAsync(id);
            });

        // The staff token always carries the user id (TokenService); the log needs who did it.
        private Guid StaffUserId()
            => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
