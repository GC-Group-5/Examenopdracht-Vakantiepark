using Vakantiepark.Shared.DTOs.Maintenance;

namespace Vakantiepark.Application.Interfaces
{
    public interface IMaintenancePeriodService
    {
        Task<MaintenanceUnitOverviewResponse> GetOverviewAsync(string accommodationUnitCode);
        Task<MaintenancePeriodSaveResult> CreateAsync(CreateMaintenancePeriodRequest request);
        Task<MaintenancePeriodSaveResult> UpdateAsync(Guid id, UpdateMaintenancePeriodRequest request);
        Task EndAsync(Guid id);
    }
}
