using Vakantiepark.Domain.Entities;
using Vakantiepark.Shared.DTOs.Park;

namespace Vakantiepark.Application.Validations.Park
{
    public static class ParkSettingsValidation
    {
        public static void Validate(this UpdateParkSettingsRequest request)
        {
            // Departure before arrival lets one guest leave and the next arrive on the same day.
            if (request.DepartureTime >= request.ArrivalTime)
                throw new ArgumentException("De vertrektijd moet vóór de aankomsttijd liggen.");

            if (request.NoRefundDaysBeforeArrival > request.FreeCancellationDaysBeforeArrival)
                throw new ArgumentException("Het aantal dagen zonder terugbetaling mag niet groter zijn dan het aantal dagen voor gratis annuleren.");
        }

        public static void Validate(this AddTouristTaxRateRequest request, IEnumerable<TouristTaxRate> existingRates)
        {
            if (existingRates.Any(rate => rate.ValidFrom == request.ValidFrom))
                throw new ArgumentException("Er is al een tarief met deze ingangsdatum.");
        }

        public static void Validate(this UpdateTypePricesRequest request, IEnumerable<AccommodationTypePrice> existingPrices)
        {
            // A price row can't be deleted (unique per type and season), so an existing price can only change.
            if (request.SeasonPrices.Any(seasonPrice => seasonPrice.PricePerNight is null && existingPrices.Any(price => price.SeasonId == seasonPrice.SeasonId)))
                throw new ArgumentException("Een seizoensprijs kan niet leeg worden gemaakt. Vul een prijs in.");
        }
    }
}
