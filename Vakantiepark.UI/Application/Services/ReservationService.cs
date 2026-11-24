using System.Globalization;
using Vakantiepark.Shared.DTOs;
using Vakantiepark.Shared.DTOs.Accommodation;
using Vakantiepark.Shared.DTOs.Booking;
using Vakantiepark.Shared.DTOs.Reservations;
using Vakantiepark.UI.Application.Extensions;
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
            List<string> query = request.ToPagingQuery();

            if (!string.IsNullOrWhiteSpace(request.Search))
                query.Add($"search={Uri.EscapeDataString(request.Search.Trim())}");
            if (request.From is DateOnly from)
                query.Add($"from={from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}");
            if (request.To is DateOnly to)
                query.Add($"to={to.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}");
            if (!string.IsNullOrWhiteSpace(request.Status))
                query.Add($"status={Uri.EscapeDataString(request.Status)}");
            if (!string.IsNullOrWhiteSpace(request.AccommodationTypeSlug))
                query.Add($"accommodationTypeSlug={Uri.EscapeDataString(request.AccommodationTypeSlug)}");

            return await HttpService.GetAsync<PagedResponse<ReservationListItemResponse>>($"/Reservations?{string.Join("&", query)}");
        }

        public async Task<ApiResponse<ReservationDetailResponse>> GetAsync(Guid id)
            => await HttpService.GetAsync<ReservationDetailResponse>($"/Reservations/{id}");

        public async Task<ApiResponse<List<AccommodationTypeResponse>>> GetTypesAsync()
            => await HttpService.GetAsync<List<AccommodationTypeResponse>>("/AccommodationType");

        public async Task<ApiResponse<List<AccommodationResponse>>> GetUnitsAsync()
            => await HttpService.GetAsync<List<AccommodationResponse>>("/Accommodation");

        public async Task<ApiResponse<ReservationDetailResponse>> ChangeAsync(Guid id, StaffChangeBookingRequest request)
            => await HttpService.PutAsync<ReservationDetailResponse, StaffChangeBookingRequest>($"/Reservations/{id}", request);

        public async Task<ApiResponse<CancellationQuoteResponse>> GetCancellationQuoteAsync(Guid id)
            => await HttpService.GetAsync<CancellationQuoteResponse>($"/Reservations/{id}/cancel/quote");

        public async Task<ApiResponse<ReservationDetailResponse>> CancelAsync(Guid id, StaffCancelBookingRequest request)
            => await HttpService.PostAsync<ReservationDetailResponse, StaffCancelBookingRequest>($"/Reservations/{id}/cancel", request);
    }
}
