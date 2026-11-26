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
            string url = request.ToPagingQuery()
                .Add("date", request.Date)
                .Add("accommodationTypeSlug", request.AccommodationTypeSlug)
                .Add("zoneId", request.ZoneId)
                .Add("category", request.Category)
                .ToUrl("/Occupancy");

            return await HttpService.GetAsync<OccupancyResponse>(url);
        }
    }
}
