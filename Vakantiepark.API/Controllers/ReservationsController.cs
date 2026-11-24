using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Vakantiepark.Application.Interfaces;
using Vakantiepark.Shared.Constants;
using Vakantiepark.Shared.DTOs;
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

        public ReservationsController(IReservationService reservationService)
        {
            ReservationService = reservationService;
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
    }
}
