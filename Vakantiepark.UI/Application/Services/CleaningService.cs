using System.Globalization;
using Vakantiepark.Shared.DTOs;
using Vakantiepark.Shared.DTOs.Cleaning;
using Vakantiepark.UI.Application.Helpers;
using Vakantiepark.UI.Application.Interfaces;

namespace Vakantiepark.UI.Application.Services
{
    public class CleaningService : ICleaningService
    {
        private readonly IHttpService HttpService;

        public CleaningService(IHttpService httpService)
        {
            HttpService = httpService;
        }

        public async Task<ApiResponse<CleaningDayResponse>> GetDayAsync(DateOnly date)
            => await HttpService.GetAsync<CleaningDayResponse>($"/Cleaning?date={date.ToString(DateFormats.Query, CultureInfo.InvariantCulture)}");

        // Start and Finish take no body; the cleaner comes from the token.
        public async Task<ApiResponse<object>> StartAsync(Guid bookingId)
            => await HttpService.PostAsync<object, object>($"/Cleaning/{bookingId}/Start", new object());

        public async Task<ApiResponse<object>> FinishAsync(Guid bookingId)
            => await HttpService.PostAsync<object, object>($"/Cleaning/{bookingId}/Finish", new object());
    }
}
