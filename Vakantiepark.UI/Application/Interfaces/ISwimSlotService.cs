using Vakantiepark.Shared.DTOs;
using Vakantiepark.Shared.DTOs.Swimming;

namespace Vakantiepark.UI.Application.Interfaces
{
    // Swim slots for a guest logged in with his booking (US-22).
    public interface ISwimSlotService
    {
        Task<ApiResponse<List<DateOnly>>> GetDaysAsync();
        Task<ApiResponse<List<SwimSlotResponse>>> GetByDateAsync(DateOnly date);
    }
}
