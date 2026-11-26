using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Vakantiepark.Application.Interfaces;
using Vakantiepark.Shared.Constants;
using Vakantiepark.Shared.DTOs;
using Vakantiepark.Shared.DTOs.Booking;
using Vakantiepark.Shared.DTOs.Reservations;

namespace Vakantiepark.API.Controllers
{
    // Staff endpoints (US-13): only medewerkers and beheerders see other people's bookings.
    [ApiController]
    [Authorize(Roles = Roles.Staff)]
    [Route("[controller]")]
    public class ReservationsController : ControllerBase
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
        {
            try
            {
                PagedResponse<ReservationListItemResponse> page = await ReservationService.SearchAsync(request);
                return Ok(ApiResponse<PagedResponse<ReservationListItemResponse>>.Ok(page));
            }
            catch (ArgumentException exception)
            {
                return BadRequest(ApiResponse<PagedResponse<ReservationListItemResponse>>.Fail(exception.Message));
            }
        }

        // US-15: who arrives and departs on this day.
        [HttpGet("Day")]
        public async Task<IActionResult> GetDayOverviewAsync([FromQuery, BindRequired] DateOnly date)
        {
            DayOverviewResponse overview = await ReservationService.GetDayOverviewAsync(date);
            return Ok(ApiResponse<DayOverviewResponse>.Ok(overview));
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetByIdAsync(Guid id)
        {
            try
            {
                ReservationDetailResponse reservation = await ReservationService.GetByIdAsync(id);
                return Ok(ApiResponse<ReservationDetailResponse>.Ok(reservation));
            }
            catch (KeyNotFoundException exception)
            {
                return NotFound(ApiResponse<ReservationDetailResponse>.Fail(exception.Message));
            }
        }

        // US-14: the same rules as for a guest, without the term; returns the updated reservation.
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> ChangeAsync(Guid id, StaffChangeBookingRequest request)
        {
            try
            {
                await BookingService.ChangeByStaffAsync(id, request, StaffUserId());
                ReservationDetailResponse reservation = await ReservationService.GetByIdAsync(id);
                return Ok(ApiResponse<ReservationDetailResponse>.Ok(reservation));
            }
            catch (KeyNotFoundException exception)
            {
                return NotFound(ApiResponse<ReservationDetailResponse>.Fail(exception.Message));
            }
            catch (ArgumentException exception)
            {
                return BadRequest(ApiResponse<ReservationDetailResponse>.Fail(exception.Message));
            }
        }

        [HttpGet("{id:guid}/Cancel/Quote")]
        public async Task<IActionResult> GetCancellationQuoteAsync(Guid id)
        {
            try
            {
                CancellationQuoteResponse quote = await BookingService.GetCancellationQuoteForStaffAsync(id);
                return Ok(ApiResponse<CancellationQuoteResponse>.Ok(quote));
            }
            catch (KeyNotFoundException exception)
            {
                return NotFound(ApiResponse<CancellationQuoteResponse>.Fail(exception.Message));
            }
            catch (ArgumentException exception)
            {
                return BadRequest(ApiResponse<CancellationQuoteResponse>.Fail(exception.Message));
            }
        }

        [HttpPost("{id:guid}/Cancel")]
        public async Task<IActionResult> CancelAsync(Guid id, StaffCancelBookingRequest request)
        {
            try
            {
                await BookingService.CancelByStaffAsync(id, request, StaffUserId());
                ReservationDetailResponse reservation = await ReservationService.GetByIdAsync(id);
                return Ok(ApiResponse<ReservationDetailResponse>.Ok(reservation));
            }
            catch (KeyNotFoundException exception)
            {
                return NotFound(ApiResponse<ReservationDetailResponse>.Fail(exception.Message));
            }
            catch (ArgumentException exception)
            {
                return BadRequest(ApiResponse<ReservationDetailResponse>.Fail(exception.Message));
            }
        }

        // The staff token always carries the user id (TokenService); the log needs who did it.
        private Guid StaffUserId()
            => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
