using Vakantiepark.Application.Interfaces;
using Vakantiepark.Shared.DTOs.Price;

namespace Vakantiepark.Application.Services
{
    // TIJDELIJK: alleen om de boekingsflow te testen. Weghalen vóór de pull request; US-07 levert de echte PriceService.
    public class TemporaryPriceService : IPriceService
    {
        public Task<PriceSpecificationResponse> CalculateAsync(string accommodationTypeSlug, DateOnly arrivalDate, DateOnly departureDate, int numberOfGuests)
            => Task.FromResult(new PriceSpecificationResponse
            {
                AccommodationPrice = 700m,
                CleaningFee = 75m,
                TouristTax = 42m,
                TotalPrice = 817m
            });
    }
}
