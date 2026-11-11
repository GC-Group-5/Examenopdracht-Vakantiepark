using Vakantiepark.Shared.DTOs.Price;

namespace Vakantiepark.Application.Interfaces
{
    /// <summary>
    /// Implemented with US-07 Prijsberekening.
    /// </summary>
    public interface IPriceService
    {
        Task<PriceSpecificationResponse> CalculateAsync(string accommodationTypeSlug, DateOnly arrivalDate, DateOnly departureDate, int numberOfGuests);
    }
}
