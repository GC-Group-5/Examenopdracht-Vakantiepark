using Vakantiepark.UI.Application.Interfaces;
using Vakantiepark.Shared.DTOs.Booking;
using Vakantiepark.Shared.DTOs;

namespace Vakantiepark.UI.Application.Services
{
    public class BookingService : IBookingService
    {
        private readonly IHttpService HttpService;

        public BookingService(IHttpService httpService)
        {
            HttpService = httpService;
        }

        public async Task<ApiResponse<BookingResponse>> CreateAsync(CreateBookingRequest request)
            => await HttpService.PostAsync<BookingResponse, CreateBookingRequest>("/Booking", request);

        public async Task<ApiResponse<BookingInfoResponse>> GetInfoAsync(string accommodationTypeSlug)
            => await HttpService.GetAsync<BookingInfoResponse>($"/Booking/info?type={Uri.EscapeDataString(accommodationTypeSlug)}");
    }
}
