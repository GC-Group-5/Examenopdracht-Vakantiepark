using Vakantiepark.Shared.DTOs;
using Vakantiepark.Shared.DTOs.Reservations;

namespace Vakantiepark.Application.Interfaces
{
    // Staff side of bookings (US-13): find any booking and look at all its details.
    public interface IReservationService
    {
        Task<PagedResponse<ReservationListItemResponse>> SearchAsync(ReservationSearchRequest request);
        Task<ReservationDetailResponse> GetByIdAsync(Guid id);
        Task<DayOverviewResponse> GetDayOverviewAsync(DateOnly date);
    }
}
