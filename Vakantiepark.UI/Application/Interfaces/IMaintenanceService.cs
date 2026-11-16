using Vakantiepark.Shared.DTOs;
using Vakantiepark.Shared.DTOs.Accommodation;
using Vakantiepark.Shared.DTOs.Maintenance;

namespace Vakantiepark.UI.Application.Interfaces
{
    // Staff screen for units that are temporarily not rentable (US-17).
    public interface IMaintenanceService
    {
        Task<ApiResponse<List<AccommodationResponse>>> GetUnitsAsync();
        Task<ApiResponse<MaintenanceUnitOverviewResponse>> GetOverviewAsync(string accommodationUnitCode);
        Task<ApiResponse<CreateMaintenancePeriodResult>> CreateAsync(CreateMaintenancePeriodRequest request);
        Task<ApiResponse<MaintenancePeriodResponse>> UpdateAsync(Guid id, UpdateMaintenancePeriodRequest request);
    }
}
