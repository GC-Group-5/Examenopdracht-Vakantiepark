using Vakantiepark.Shared.DTOs;
using Vakantiepark.Shared.DTOs.Accommodation;
using Vakantiepark.Shared.DTOs.Booking;
using Vakantiepark.Shared.DTOs.Reservations;
using Vakantiepark.UI.Application.Extensions;
using Vakantiepark.UI.Application.Helpers;
using Vakantiepark.UI.Application.Interfaces;

namespace Vakantiepark.UI.Application.Services
{
    public class ReservationService : IReservationService
    {
        private readonly IHttpService HttpService;

        public ReservationService(IHttpService httpService)
        {
            HttpService = httpService;
        }

        public async Task<ApiResponse<PagedResponse<ReservationListItemResponse>>> SearchAsync(ReservationSearchRequest request)
        {
            string url = request.ToPagingQuery()
                .Add("search", request.Search)
                .Add("from", request.From)
                .Add("to", request.To)
                .Add("status", request.Status)
                .Add("accommodationTypeSlug", request.AccommodationTypeSlug)
                .ToUrl("/Reservations");

            return await HttpService.GetAsync<PagedResponse<ReservationListItemResponse>>(url);
        }

        public async Task<ApiResponse<ReservationDetailResponse>> GetAsync(Guid id)
            => await HttpService.GetAsync<ReservationDetailResponse>($"/Reservations/{id}");

        public async Task<ApiResponse<DayOverviewResponse>> GetDayOverviewAsync(DateOnly date)
            => await HttpService.GetAsync<DayOverviewResponse>(new QueryString().Add("date", date).ToUrl("/Reservations/Day"));

        public async Task<ApiResponse<List<AccommodationTypeResponse>>> GetTypesAsync()
            => await HttpService.GetAsync<List<AccommodationTypeResponse>>("/AccommodationType");

        public async Task<ApiResponse<ChangeOptionsResponse>> GetChangeOptionsAsync(Guid id, DateOnly arrivalDate, DateOnly departureDate, int numberOfGuests)
        {
            string url = new QueryString()
                .Add("arrivalDate", arrivalDate)
                .Add("departureDate", departureDate)
                .Add("numberOfGuests", numberOfGuests)
                .ToUrl($"/Reservations/{id}/ChangeOptions");

            return await HttpService.GetAsync<ChangeOptionsResponse>(url);
        }

        public async Task<ApiResponse<ReservationDetailResponse>> ChangeAsync(Guid id, StaffChangeBookingRequest request)
            => await HttpService.PutAsync<ReservationDetailResponse, StaffChangeBookingRequest>($"/Reservations/{id}", request);

        public async Task<ApiResponse<CancellationQuoteResponse>> GetCancellationQuoteAsync(Guid id)
            => await HttpService.GetAsync<CancellationQuoteResponse>($"/Reservations/{id}/Cancel/Quote");

        public async Task<ApiResponse<ReservationDetailResponse>> CancelAsync(Guid id, StaffCancelBookingRequest request)
            => await HttpService.PostAsync<ReservationDetailResponse, StaffCancelBookingRequest>($"/Reservations/{id}/Cancel", request);
    }
}
