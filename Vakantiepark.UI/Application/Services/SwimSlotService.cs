using System.Globalization;
using Vakantiepark.Shared.DTOs;
using Vakantiepark.Shared.DTOs.Swimming;
using Vakantiepark.UI.Application.Interfaces;

namespace Vakantiepark.UI.Application.Services
{
    public class SwimSlotService : ISwimSlotService
    {
        private readonly IHttpService HttpService;

        public SwimSlotService(IHttpService httpService)
        {
            HttpService = httpService;
        }

        public async Task<ApiResponse<List<DateOnly>>> GetDaysAsync()
            => await HttpService.GetAsync<List<DateOnly>>("/SwimSlot/Days");

        public async Task<ApiResponse<List<SwimSlotResponse>>> GetByDateAsync(DateOnly date)
            => await HttpService.GetAsync<List<SwimSlotResponse>>($"/SwimSlot?date={date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}");
    }
}
