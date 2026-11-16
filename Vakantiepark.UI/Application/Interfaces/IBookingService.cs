using Vakantiepark.Shared.DTOs.Booking;
using Vakantiepark.Shared.DTOs;

namespace Vakantiepark.UI.Application.Interfaces
{
    public interface IBookingService
    {
        Task<ApiResponse<BookingQuoteResponse>> GetQuoteAsync(string accommodationTypeSlug, DateOnly arrivalDate, DateOnly departureDate, int numberOfGuests, int numberOfPets, bool wheelchairAccessible, string? preferredZone);
        Task<ApiResponse<BookingResponse>> CreateAsync(CreateBookingRequest request);
        Task<ApiResponse<MyBookingResponse>> GetMineByTokenAsync(string token);
        Task<ApiResponse<MyBookingResponse>> GetMineByNumberAndEmailAsync(FindMyBookingRequest request);
        Task<ApiResponse<CancellationQuoteResponse>> GetCancellationQuoteAsync(BookingAccessRequest access);
        Task<ApiResponse<MyBookingResponse>> CancelAsync(BookingAccessRequest access);
        Task<ApiResponse<ChangeQuoteResponse>> GetChangeQuoteAsync(ChangeBookingRequest request);
        Task<ApiResponse<MyBookingResponse>> ChangeAsync(ChangeBookingRequest request);
    }
}
