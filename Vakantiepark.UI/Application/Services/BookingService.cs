using Vakantiepark.UI.Application.Helpers;
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

        public async Task<ApiResponse<BookingQuoteResponse>> GetQuoteAsync(string accommodationTypeSlug, DateOnly arrivalDate, DateOnly departureDate, int numberOfGuests, int numberOfChildrenUnder12, int numberOfPets, bool wheelchairAccessible, string? preferredZone)
        {
            string url = new QueryString()
                .Add("accommodationTypeSlug", accommodationTypeSlug)
                .Add("arrivalDate", arrivalDate)
                .Add("departureDate", departureDate)
                .Add("numberOfGuests", numberOfGuests)
                .Add("numberOfChildrenUnder12", numberOfChildrenUnder12)
                .Add("numberOfPets", numberOfPets)
                .Add("wheelchairAccessible", wheelchairAccessible)
                .Add("preferredZone", preferredZone)
                .ToUrl("/Booking/quote");

            return await HttpService.GetAsync<BookingQuoteResponse>(url);
        }

        public async Task<ApiResponse<BookingResponse>> CreateAsync(CreateBookingRequest request)
            => await HttpService.PostAsync<BookingResponse, CreateBookingRequest>("/Booking", request);

        public async Task<ApiResponse<MyBookingResponse>> GetMineByTokenAsync(string token)
            => await HttpService.GetAsync<MyBookingResponse>($"/Booking/mine/{Uri.EscapeDataString(token)}");

        // Needs the guest token from /Auth/Guest; the token handler adds it to the request.
        public async Task<ApiResponse<MyBookingResponse>> GetMineAsync()
            => await HttpService.GetAsync<MyBookingResponse>("/Booking/mine");

        // Without a link code the guest login token (added by the token handler) says which booking it is.
        public async Task<ApiResponse<CancellationQuoteResponse>> GetCancellationQuoteAsync(BookingAccessRequest access)
            => await HttpService.PostAsync<CancellationQuoteResponse, BookingAccessRequest>("/Booking/mine/cancel/quote", access);

        public async Task<ApiResponse<MyBookingResponse>> CancelAsync(BookingAccessRequest access)
            => await HttpService.PostAsync<MyBookingResponse, BookingAccessRequest>("/Booking/mine/cancel", access);

        public async Task<ApiResponse<ChangeQuoteResponse>> GetChangeQuoteAsync(ChangeBookingRequest request)
            => await HttpService.PostAsync<ChangeQuoteResponse, ChangeBookingRequest>("/Booking/mine/change/quote", request);

        public async Task<ApiResponse<MyBookingResponse>> ChangeAsync(ChangeBookingRequest request)
            => await HttpService.PostAsync<MyBookingResponse, ChangeBookingRequest>("/Booking/mine/change", request);
    }
}
