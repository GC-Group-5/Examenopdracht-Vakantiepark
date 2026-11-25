using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Vakantiepark.Application.Interfaces;
using Vakantiepark.Shared.Constants;
using Vakantiepark.Shared.DTOs.Maintenance;


namespace Vakantiepark.API.Controllers
{
    // Staff endpoints (US-17): only medewerkers and beheerders manage maintenance periods.
    [ApiController]
    [Authorize(Roles = Roles.Staff)]
    [Route("[controller]")]
    public class MaintenancePeriodsController : ApiControllerBase
    {
        private readonly IMaintenancePeriodService MaintenanceService;

        public MaintenancePeriodsController(IMaintenancePeriodService maintenanceService)
        {
            MaintenanceService = maintenanceService;
        }
        [HttpGet]
        public async Task<IActionResult> GetOverviewAsync([FromQuery] string accommodationUnitCode)
            => await RunAsync(() => MaintenanceService.GetOverviewAsync(accommodationUnitCode));

        [HttpPost]
        public async Task<IActionResult> CreateAsync([FromBody] CreateMaintenancePeriodRequest request)
            => await RunAsync(() => MaintenanceService.CreateAsync(request));

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateAsync(Guid id, [FromBody] UpdateMaintenancePeriodRequest request)
            => await RunAsync(() => MaintenanceService.UpdateAsync(id, request));

        // Ends (running) or cancels (planned) a period: it is removed and the unit is bookable from today.
        [HttpPost("{id:guid}/End")]
        public async Task<IActionResult> EndAsync(Guid id)
            => await RunAsync(() => MaintenanceService.EndAsync(id));
    }
}
