using Vakantiepark.Shared.DTOs.Maintenance;

namespace Vakantiepark.Application.Interfaces
{
    public interface IMaintenancePeriodService
    {
        Task<MaintenanceUnitOverviewResponse> GetOverviewAsync(string accommodationUnitCode);
        Task<CreateMaintenancePeriodResult> CreateAsync(CreateMaintenancePeriodRequest request);
        Task<MaintenancePeriodResponse> UpdateAsync(Guid id, UpdateMaintenancePeriodRequest request);
    }
}
