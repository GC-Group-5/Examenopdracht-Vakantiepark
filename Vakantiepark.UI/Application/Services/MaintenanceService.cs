using Vakantiepark.Shared.DTOs;
using Vakantiepark.Shared.DTOs.Accommodation;
using Vakantiepark.Shared.DTOs.Maintenance;
using Vakantiepark.UI.Application.Helpers;
using Vakantiepark.UI.Application.Interfaces;

namespace Vakantiepark.UI.Application.Services
{
    public class MaintenanceService : IMaintenanceService
    {
        private readonly IHttpService HttpService;

        public MaintenanceService(IHttpService httpService)
        {
            HttpService = httpService;
        }

        public async Task<ApiResponse<List<AccommodationResponse>>> GetUnitsAsync()
            => await HttpService.GetAsync<List<AccommodationResponse>>("/Accommodation");

        public async Task<ApiResponse<MaintenanceUnitOverviewResponse>> GetOverviewAsync(string accommodationUnitCode)
            => await HttpService.GetAsync<MaintenanceUnitOverviewResponse>(new QueryString().Add("accommodationUnitCode", accommodationUnitCode).ToUrl("/MaintenancePeriods"));

        public async Task<ApiResponse<MaintenancePeriodSaveResult>> CreateAsync(CreateMaintenancePeriodRequest request)
            => await HttpService.PostAsync<MaintenancePeriodSaveResult, CreateMaintenancePeriodRequest>("/MaintenancePeriods", request);

        public async Task<ApiResponse<MaintenancePeriodSaveResult>> UpdateAsync(Guid id, UpdateMaintenancePeriodRequest request)
            => await HttpService.PutAsync<MaintenancePeriodSaveResult, UpdateMaintenancePeriodRequest>($"/MaintenancePeriods/{id}", request);

        public async Task<ApiResponse<object>> EndAsync(Guid id)
            => await HttpService.PostAsync<object, object>($"/MaintenancePeriods/{id}/End", new object());
    }
}
