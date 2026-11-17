using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Vakantiepark.Application.Interfaces;
using Vakantiepark.Shared.DTOs;
using Vakantiepark.Shared.Constants;
using Vakantiepark.Shared.DTOs.Maintenance;


namespace Vakantiepark.API.Controllers
{
    // Staff endpoints (US-17): only medewerkers and beheerders manage maintenance periods.
    [ApiController]
    [Authorize(Roles = Roles.Staff)]
    [Route("[controller]")]
    public class MaintenancePeriodsController : ControllerBase
    {
        private readonly IMaintenancePeriodService MaintenanceService;

        public MaintenancePeriodsController(IMaintenancePeriodService maintenanceService)
        {
            MaintenanceService = maintenanceService;
        }
        [HttpGet]
        public async Task<IActionResult> GetOverviewAsync([FromQuery] string accommodationUnitCode)
        {
            try
            {
                MaintenanceUnitOverviewResponse overview = await MaintenanceService.GetOverviewAsync(accommodationUnitCode);
                return Ok(ApiResponse<MaintenanceUnitOverviewResponse>.Ok(overview));
            }
            catch (ArgumentException exception)
            {
                return BadRequest(ApiResponse<MaintenanceUnitOverviewResponse>.Fail(exception.Message));
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateAsync([FromBody] CreateMaintenancePeriodRequest request)
        {
            try
            {
                MaintenancePeriodSaveResult result = await MaintenanceService.CreateAsync(request);
                return Ok(ApiResponse<MaintenancePeriodSaveResult>.Ok(result));
            }
            catch (ArgumentException exception)
            {
                return BadRequest(ApiResponse<MaintenancePeriodSaveResult>.Fail(exception.Message));
            }
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateAsync(Guid id, [FromBody] UpdateMaintenancePeriodRequest request)
        {
            try
            {
                MaintenancePeriodSaveResult result = await MaintenanceService.UpdateAsync(id, request);
                return Ok(ApiResponse<MaintenancePeriodSaveResult>.Ok(result));
            }
            catch (ArgumentException exception)
            {
                return BadRequest(ApiResponse<MaintenancePeriodSaveResult>.Fail(exception.Message));
            }
        }

        // Ends (running) or cancels (planned) a period: it is removed and the unit is bookable from today.
        [HttpPost("{id:guid}/End")]
        public async Task<IActionResult> EndAsync(Guid id)
        {
            try
            {
                await MaintenanceService.EndAsync(id);
                return Ok(ApiResponse<object>.Ok());
            }
            catch (ArgumentException exception)
            {
                return BadRequest(ApiResponse<object>.Fail(exception.Message));
            }
        }
    }
}
