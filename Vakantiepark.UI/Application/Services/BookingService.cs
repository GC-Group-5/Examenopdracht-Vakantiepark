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

        public async Task<ApiResponse<BookingQuoteResponse>> GetQuoteAsync(string accommodationTypeSlug, DateOnly arrivalDate, DateOnly departureDate, int numberOfGuests, int numberOfPets, bool wheelchairAccessible, string? preferredZone)
        {
            string url = "/Booking/quote"
                + $"?accommodationTypeSlug={Uri.EscapeDataString(accommodationTypeSlug)}"
                + $"&arrivalDate={arrivalDate:yyyy-MM-dd}"
                + $"&departureDate={departureDate:yyyy-MM-dd}"
                + $"&numberOfGuests={numberOfGuests}"
                + $"&numberOfPets={numberOfPets}";
            if (wheelchairAccessible)
                url += "&wheelchairAccessible=true";
            if (!string.IsNullOrWhiteSpace(preferredZone))
                url += $"&preferredZone={Uri.EscapeDataString(preferredZone)}";

            return await HttpService.GetAsync<BookingQuoteResponse>(url);
        }

        public async Task<ApiResponse<BookingResponse>> CreateAsync(CreateBookingRequest request)
            => await HttpService.PostAsync<BookingResponse, CreateBookingRequest>("/Booking", request);

        public async Task<ApiResponse<MyBookingResponse>> GetMineByTokenAsync(string token)
            => await HttpService.GetAsync<MyBookingResponse>($"/Booking/mine/{Uri.EscapeDataString(token)}");

        // Needs the guest token from /Auth/Guest; the token handler adds it to the request.
        public async Task<ApiResponse<MyBookingResponse>> GetMineAsync()
            => await HttpService.GetAsync<MyBookingResponse>("/Booking/mine");
    }
}
