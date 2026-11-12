using Vakantiepark.Shared.DTOs.Booking;
using Vakantiepark.Domain.Entities;
using Vakantiepark.Domain.Enums;
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
                NumberOfGuests = 4,
                Name = "Jan Jansen",
                BirthDate = new DateOnly(1985, 3, 14),
                Email = "jan@example.nl",
                PhoneNumber = "0612345678",
                Travelers =
                [
                    new TravelerRequest() { Name = "Petra Jansen", BirthDate = new DateOnly(1987, 6, 2) },
                    new TravelerRequest() { Name = "Sanne Jansen", BirthDate = new DateOnly(2015, 9, 21) },
                    new TravelerRequest() { Name = "Tim Jansen", BirthDate = new DateOnly(2018, 1, 5) }
                ]
            };

            configure?.Invoke(request);
            return request;
        }

        public static User CreateUser(Action<User>? configure = null)
        {
            User user = new User()
            {
                Id = Guid.NewGuid(),
                FullName = "Mila Medewerker",
                Email = "medewerker@dropuut.example",
                PasswordHash = "hash",
                Role = UserRole.Medewerker,
                IsActive = true
            };

            configure?.Invoke(user);
            return user;
        }

        public static Booking CreateBooking(DateOnly today, Action<Booking>? configure = null)
        {
            Booking booking = new Booking()
            {
                Id = Guid.NewGuid(),
                BookingNumber = "DOU-7KQ3M9XP",
                Name = "Jan Jansen",
                Email = "jan@example.nl",
                PhoneNumber = "0612345678",
                ArrivalDate = today.AddDays(2),
                DepartureDate = today.AddDays(9),
                NumberOfGuests = 4
            };

            configure?.Invoke(booking);
            return booking;
        }

        public static ParkSettings CreateParkSettings(Action<ParkSettings>? configure = null)
        {
            ParkSettings settings = new ParkSettings()
            {
                Id = Guid.NewGuid(),
                ArrivalTime = new TimeOnly(15, 0),
                DepartureTime = new TimeOnly(10, 0),
                HighSeasonMinimumNights = 7,
                DepositPercentage = 30m,
                FreeCancellationDaysBeforeArrival = 42,
                NoRefundDaysBeforeArrival = 14
            };

            configure?.Invoke(settings);
            return settings;
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

        public static Season CreateSeason(Action<Season>? configure = null)
        {
            Season season = new()
            {
                Id = Guid.NewGuid(),
                Name = "Hoogseizoen testjaar",
                StartDate = new DateOnly(2026, 7, 1),
                EndDate = new DateOnly(2026, 8, 31),
            };

            configure?.Invoke(season);
            return season;
        }

        public static AccommodationType CreateAccommodationType(Action<AccommodationType>? configure = null)
        {
            AccommodationType type = new()
            {
                Id = Guid.NewGuid(),
                Slug = "berk-4",
                Name = "Berk 4",
                Category = AccommodationCategory.HolidayHome,
            };

            configure?.Invoke(type);
            return type;
        }
    }
}
