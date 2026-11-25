using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vakantiepark.Application.Interfaces;
using Vakantiepark.Shared.Constants;
using Vakantiepark.Shared.DTOs;
using Vakantiepark.Shared.DTOs.Park;

namespace Vakantiepark.API.Controllers
{
    // US-18: rates and park rules are for the beheerder only; a medewerker gets 403.
    [ApiController]
    [Authorize(Roles = Roles.Beheerder)]
    [Route("[controller]")]
    public class ParkSettingsController : ControllerBase
    {
        private readonly IParkSettingsService ParkSettingsService;

        public ParkSettingsController(IParkSettingsService parkSettingsService)
        {
            ParkSettingsService = parkSettingsService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAsync()
            => Ok(ApiResponse<ParkSettingsResponse>.Ok(await ParkSettingsService.GetAsync()));

        [HttpPut]
        public async Task<IActionResult> UpdateAsync(UpdateParkSettingsRequest request)
            => await SaveAsync(() => ParkSettingsService.UpdateAsync(request), "De parkregels zijn opgeslagen.");

        [HttpPost("TouristTaxRates")]
        public async Task<IActionResult> AddTouristTaxRateAsync(AddTouristTaxRateRequest request)
            => await SaveAsync(() => ParkSettingsService.AddTouristTaxRateAsync(request), "Het nieuwe tarief is toegevoegd.");

        [HttpPost("Seasons")]
        public async Task<IActionResult> CreateSeasonAsync(SaveSeasonRequest request)
            => await SaveAsync(() => ParkSettingsService.CreateSeasonAsync(request), "Het seizoen is toegevoegd.");

        [HttpPut("Seasons/{id:guid}")]
        public async Task<IActionResult> UpdateSeasonAsync(Guid id, SaveSeasonRequest request)
            => await SaveAsync(() => ParkSettingsService.UpdateSeasonAsync(id, request), "Het seizoen is opgeslagen.");

        [HttpDelete("Seasons/{id:guid}")]
        public async Task<IActionResult> DeleteSeasonAsync(Guid id)
            => await SaveAsync(() => ParkSettingsService.DeleteSeasonAsync(id), "Het seizoen is verwijderd.");

        [HttpPut("Prices/{slug}")]
        public async Task<IActionResult> UpdateTypePricesAsync(string slug, UpdateTypePricesRequest request)
            => await SaveAsync(() => ParkSettingsService.UpdateTypePricesAsync(slug, request), "De prijzen zijn opgeslagen.");

        // Every change returns the same envelope and maps the same exceptions.
        private async Task<IActionResult> SaveAsync(Func<Task> save, string message)
        {
            try
            {
                await save();
                return Ok(ApiResponse<object>.Ok(message: message));
            }
            catch (ArgumentException exception)
            {
                return BadRequest(ApiResponse<object>.Fail(exception.Message));
            }
            catch (KeyNotFoundException exception)
            {
                return NotFound(ApiResponse<object>.Fail(exception.Message));
            }
        }
    }
}
