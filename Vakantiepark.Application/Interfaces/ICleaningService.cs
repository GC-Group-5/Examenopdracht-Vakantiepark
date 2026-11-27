using Vakantiepark.Shared.DTOs.Cleaning;

namespace Vakantiepark.Application.Interfaces
{
    public interface ICleaningService
    {
        // Without a date the overview shows today.
        Task<CleaningDayResponse> GetDayAsync(DateOnly? date);

        // The cleaner marks a unit as busy, and later as done. Both store who did it.
        Task StartAsync(Guid bookingId, Guid userId);
        Task FinishAsync(Guid bookingId, Guid userId);
    }
}
