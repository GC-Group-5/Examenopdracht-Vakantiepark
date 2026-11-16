using Vakantiepark.Shared.DTOs;
using Vakantiepark.Shared.DTOs.Accommodation;
using Vakantiepark.Shared.DTOs.Maintenance;
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
            => await HttpService.GetAsync<MaintenanceUnitOverviewResponse>($"/MaintenancePeriods?accommodationUnitCode={Uri.EscapeDataString(accommodationUnitCode)}");

        public async Task<ApiResponse<CreateMaintenancePeriodResult>> CreateAsync(CreateMaintenancePeriodRequest request)
            => await HttpService.PostAsync<CreateMaintenancePeriodResult, CreateMaintenancePeriodRequest>("/MaintenancePeriods", request);

        public async Task<ApiResponse<MaintenancePeriodResponse>> UpdateAsync(Guid id, UpdateMaintenancePeriodRequest request)
            => await HttpService.PutAsync<MaintenancePeriodResponse, UpdateMaintenancePeriodRequest>($"/MaintenancePeriods/{id}", request);
    }
}
