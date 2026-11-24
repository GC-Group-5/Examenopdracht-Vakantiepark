using Vakantiepark.Shared.DTOs;
using Vakantiepark.Shared.DTOs.Accommodation;
using Vakantiepark.Shared.DTOs.Reservations;

namespace Vakantiepark.UI.Application.Interfaces
{
    // Staff screens for all bookings (US-13) and the day overview (US-15).
    public interface IReservationService
    {
        Task<ApiResponse<PagedResponse<ReservationListItemResponse>>> SearchAsync(ReservationSearchRequest request);
        Task<ApiResponse<ReservationDetailResponse>> GetAsync(Guid id);
        Task<ApiResponse<DayOverviewResponse>> GetDayOverviewAsync(DateOnly date);
        Task<ApiResponse<List<AccommodationTypeResponse>>> GetTypesAsync();
    }
}
