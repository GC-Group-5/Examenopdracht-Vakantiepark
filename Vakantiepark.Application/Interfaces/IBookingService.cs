using Vakantiepark.Shared.DTOs.Booking;

namespace Vakantiepark.Application.Interfaces
{
    public interface IBookingService
    {
        Task<BookingResponse> CreateAsync(CreateBookingRequest request);
        Task<BookingInfoResponse> GetInfoAsync(string accommodationTypeSlug);
    }
}
