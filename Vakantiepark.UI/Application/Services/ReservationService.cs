using System.Globalization;
using Vakantiepark.Shared.DTOs;
using Vakantiepark.Shared.DTOs.Accommodation;
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

        public async Task<ApiResponse<DayOverviewResponse>> GetDayOverviewAsync(DateOnly date)
            => await HttpService.GetAsync<DayOverviewResponse>($"/Reservations/Day?date={date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}");

        public async Task<ApiResponse<List<AccommodationTypeResponse>>> GetTypesAsync()
            => await HttpService.GetAsync<List<AccommodationTypeResponse>>("/AccommodationType");
    }
}
