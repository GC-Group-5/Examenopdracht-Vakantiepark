using Vakantiepark.Shared.DTOs.Swimming;

namespace Vakantiepark.Application.Interfaces
{
    public interface ISwimSlotService
    {
        Task<List<DateOnly>> GetDaysAsync(Guid bookingId);
        Task<List<SwimSlotResponse>> GetByDateAsync(Guid bookingId, DateOnly date);
    }
}
