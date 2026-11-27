using Vakantiepark.Shared.DTOs.Cleaning;

namespace Vakantiepark.Application.Interfaces
{
    public interface ICleaningService
    {
        // Without a date the overview shows today.
        Task<CleaningDayResponse> GetDayAsync(DateOnly? date);
    }
}
