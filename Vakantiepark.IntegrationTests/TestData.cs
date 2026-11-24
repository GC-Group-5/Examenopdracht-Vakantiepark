using Vakantiepark.Domain.Entities;
using Vakantiepark.Domain.Enums;

namespace Vakantiepark.IntegrationTests
{
    public static class TestData
    {
        // A unit with its own type and zone, so tests never see each other's data.
        public static Accommodation CreateUnit()
        {
            string token = Guid.NewGuid().ToString("N")[..8];
            return new Accommodation
            {
                Id = Guid.NewGuid(),
                Code = $"T-{token}",
                Name = $"Test {token}",
                MaxPersons = 4,
                Type = new AccommodationType { Id = Guid.NewGuid(), Slug = $"t-{token}", Name = $"Type {token}", Category = AccommodationCategory.HolidayHome },
                Zone = new Zone { Id = Guid.NewGuid(), Name = $"Z{token}", Category = AccommodationCategory.HolidayHome },
            };
        }

        public static Booking CreateBooking(Guid unitId, DateOnly arrival, DateOnly departure, Action<Booking>? configure = null)
        {
            Booking booking = new Booking
            {
                Id = Guid.NewGuid(),
                BookingNumber = Guid.NewGuid().ToString("N")[..12],
                AccessToken = Guid.NewGuid().ToString("N"),
                Name = "Test Gast",
                Email = "test@example.com",
                PhoneNumber = "0612345678",
                ArrivalDate = arrival,
                DepartureDate = departure,
                NumberOfGuests = 2,
                AccommodationUnitId = unitId,
                CreatedAt = DateTime.UtcNow,
            };

            configure?.Invoke(booking);
            return booking;
        }
    }
}
