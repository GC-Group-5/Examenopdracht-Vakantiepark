using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vakantiepark.Application.Interfaces;
using Vakantiepark.Shared.Constants;
using Vakantiepark.Shared.DTOs.Park;

namespace Vakantiepark.API.Controllers
{
    // US-18: rates and park rules are for the beheerder only; a medewerker gets 403.
    [ApiController]
    [Authorize(Roles = Roles.Beheerder)]
    [Route("[controller]")]
    public class ParkSettingsController : ApiControllerBase
    {
        private readonly IParkSettingsService ParkSettingsService;

        public ParkSettingsController(IParkSettingsService parkSettingsService)
        {
            ParkSettingsService = parkSettingsService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAsync()
            => await RunAsync(() => ParkSettingsService.GetAsync());

        [HttpPut]
        public async Task<IActionResult> UpdateAsync(UpdateParkSettingsRequest request)
            => await RunAsync(() => ParkSettingsService.UpdateAsync(request), "De parkregels zijn opgeslagen.");

        [HttpPost("TouristTaxRates")]
        public async Task<IActionResult> AddTouristTaxRateAsync(AddTouristTaxRateRequest request)
            => await RunAsync(() => ParkSettingsService.AddTouristTaxRateAsync(request), "Het nieuwe tarief is toegevoegd.");

        [HttpPost("Seasons")]
        public async Task<IActionResult> CreateSeasonAsync(SaveSeasonRequest request)
            => await RunAsync(() => ParkSettingsService.CreateSeasonAsync(request), "Het seizoen is toegevoegd.");

        [HttpPut("Seasons/{id:guid}")]
        public async Task<IActionResult> UpdateSeasonAsync(Guid id, SaveSeasonRequest request)
            => await RunAsync(() => ParkSettingsService.UpdateSeasonAsync(id, request), "Het seizoen is opgeslagen.");

        [HttpDelete("Seasons/{id:guid}")]
        public async Task<IActionResult> DeleteSeasonAsync(Guid id)
            => await RunAsync(() => ParkSettingsService.DeleteSeasonAsync(id), "Het seizoen is verwijderd.");

        [HttpPut("Prices/{slug}")]
        public async Task<IActionResult> UpdateTypePricesAsync(string slug, UpdateTypePricesRequest request)
            => await RunAsync(() => ParkSettingsService.UpdateTypePricesAsync(slug, request), "De prijzen zijn opgeslagen.");
    }
}
