using Vakantiepark.Shared.DTOs.Booking;
using Vakantiepark.Shared.DTOs.Price;

namespace Vakantiepark.UnitTests
{
    public static class TestData
    {
        public static CreateBookingRequest CreateNewBookingRequest(Action<CreateBookingRequest>? configure = null)
        {
            DateOnly arrivalDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30);

            CreateBookingRequest request = new CreateBookingRequest()
            {
                AccommodationTypeSlug = "eik-luxe-6",
                ArrivalDate = arrivalDate,
                DepartureDate = arrivalDate.AddDays(7),
                ArrivalTime = new TimeOnly(15, 0),
                DepartureTime = new TimeOnly(10, 0),
                NumberOfGuests = 4,
                Name = "Jan Jansen",
                Email = "jan@example.nl",
                PhoneNumber = "0612345678"
            };

            configure?.Invoke(request);
            return request;
        }

        public static PriceSpecificationResponse CreatePriceSpecification(Action<PriceSpecificationResponse>? configure = null)
        {
            PriceSpecificationResponse price = new PriceSpecificationResponse()
            {
                AccommodationPrice = 700m,
                CleaningFee = 75m,
                TouristTax = 42m,
                TotalPrice = 817m
            };

            configure?.Invoke(price);
            return price;
        }
    }
}
