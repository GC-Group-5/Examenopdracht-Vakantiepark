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
                CreateMaintenancePeriodResult result = await MaintenanceService.CreateAsync(request);
                return Ok(ApiResponse<CreateMaintenancePeriodResult>.Ok(result));
            }
            catch (ArgumentException exception)
            {
                return BadRequest(ApiResponse<CreateMaintenancePeriodResult>.Fail(exception.Message));
            }
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateAsync(Guid id, [FromBody] UpdateMaintenancePeriodRequest request)
        {
            try
            {
                MaintenancePeriodResponse period = await MaintenanceService.UpdateAsync(id, request);
                return Ok(ApiResponse<MaintenancePeriodResponse>.Ok(period));
            }
            catch (ArgumentException exception)
            {
                return BadRequest(ApiResponse<MaintenancePeriodResponse>.Fail(exception.Message));
            }
        }
    }
}
