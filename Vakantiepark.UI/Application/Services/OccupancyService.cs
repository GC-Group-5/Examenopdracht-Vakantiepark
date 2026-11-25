using System.Globalization;
using Vakantiepark.Shared.DTOs;
using Vakantiepark.Shared.DTOs.Occupancy;
using Vakantiepark.UI.Application.Extensions;
using Vakantiepark.UI.Application.Interfaces;

namespace Vakantiepark.UI.Application.Services
{
    public class OccupancyService : IOccupancyService
    {
        private readonly IHttpService HttpService;

        public OccupancyService(IHttpService httpService)
        {
            HttpService = httpService;
        }

        public async Task<ApiResponse<OccupancyResponse>> GetAsync(OccupancyRequest request)
        {
            List<string> query = request.ToPagingQuery();
            if (request.Date is DateOnly date)
                query.Add($"date={date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}");

            if (!string.IsNullOrWhiteSpace(request.AccommodationTypeSlug))
                query.Add($"accommodationTypeSlug={Uri.EscapeDataString(request.AccommodationTypeSlug)}");

            if (request.ZoneId is Guid zoneId)
                query.Add($"zoneId={zoneId}");

            if (!string.IsNullOrWhiteSpace(request.Category))
                query.Add($"category={Uri.EscapeDataString(request.Category)}");

            return await HttpService.GetAsync<OccupancyResponse>($"/Occupancy?{string.Join("&", query)}");
        }
    }
}
