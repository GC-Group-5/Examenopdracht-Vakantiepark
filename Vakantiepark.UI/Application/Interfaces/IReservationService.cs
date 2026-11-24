using Vakantiepark.Shared.DTOs;
using Vakantiepark.Shared.DTOs.Accommodation;
using Vakantiepark.Shared.DTOs.Booking;
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

        // US-14: staff changes or cancels a booking, without the guest's term.
        Task<ApiResponse<List<AccommodationResponse>>> GetUnitsAsync();
        Task<ApiResponse<ReservationDetailResponse>> ChangeAsync(Guid id, StaffChangeBookingRequest request);
        Task<ApiResponse<CancellationQuoteResponse>> GetCancellationQuoteAsync(Guid id);
        Task<ApiResponse<ReservationDetailResponse>> CancelAsync(Guid id, StaffCancelBookingRequest request);
    }
}
