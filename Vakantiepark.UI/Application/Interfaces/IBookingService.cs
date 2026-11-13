using Vakantiepark.Shared.DTOs.Booking;
using Vakantiepark.Shared.DTOs;

namespace Vakantiepark.UI.Application.Interfaces
{
    public interface IBookingService
    {
        Task<ApiResponse<BookingResponse>> CreateAsync(CreateBookingRequest request);
        Task<ApiResponse<BookingInfoResponse>> GetInfoAsync(string accommodationTypeSlug);
    }
}
