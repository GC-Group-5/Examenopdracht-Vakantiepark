using Vakantiepark.Domain.Entities;
using Vakantiepark.Domain.Enums;
using Vakantiepark.Infrastructure.Persistence;
using Vakantiepark.Infrastructure.Repositories;

namespace Vakantiepark.IntegrationTests
{
    public class ConcurrentBookingTests : IClassFixture<PostgresFixture>
    {
        private readonly PostgresFixture Database;

        public ConcurrentBookingTests(PostgresFixture database)
        {
            Database = database;
        }

        [Fact]
        public async Task TryAddAsync_TwoOverlappingBookingsAtTheSameTime_ExactlyOneSucceeds()
        {
            Guid unitId = await CreateUnitAsync();

            bool[] results = await BookAtTheSameTimeAsync(
                NewBooking(unitId, new DateOnly(2027, 7, 10), new DateOnly(2027, 7, 17)),
                NewBooking(unitId, new DateOnly(2027, 7, 12), new DateOnly(2027, 7, 19)));

            Assert.Equal(1, results.Count(result => result));
        }

        [Fact]
        public async Task TryAddAsync_DepartureDayIsArrivalDay_BothSucceed()
        {
            Guid unitId = await CreateUnitAsync();

            bool[] results = await BookAtTheSameTimeAsync(
                NewBooking(unitId, new DateOnly(2027, 7, 10), new DateOnly(2027, 7, 17)),
                NewBooking(unitId, new DateOnly(2027, 7, 17), new DateOnly(2027, 7, 24)));

            Assert.All(results, Assert.True);
        }

        [Fact]
        public async Task TryAddAsync_OverlapsDeletedBooking_Succeeds()
        {
            Guid unitId = await CreateUnitAsync();
            await using (ApplicationDBContext context = Database.CreateContext())
            {
                Booking cancelled = NewBooking(unitId, new DateOnly(2027, 8, 1), new DateOnly(2027, 8, 8));
                cancelled.DeletedAt = DateTime.UtcNow;
                context.Bookings.Add(cancelled);
                await context.SaveChangesAsync();
            }

            await using ApplicationDBContext newContext = Database.CreateContext();
            bool result = await new BookingRepository(newContext).TryAddAsync(
                NewBooking(unitId, new DateOnly(2027, 8, 3), new DateOnly(2027, 8, 10)));

            Assert.True(result);
        }

        // Een eigen eenheid per test, zodat tests elkaar niet storen.
        private async Task<Guid> CreateUnitAsync()
        {
            await using ApplicationDBContext context = Database.CreateContext();
            Guid id = Guid.NewGuid();
            string suffix = id.ToString("N")[..8];
            context.Accommodations.Add(new Accommodation
            {
                Id = id,
                Code = $"T-{suffix}",
                Name = $"Test {suffix}",
                MaxPersons = 4,
                Type = new AccommodationType { Id = Guid.NewGuid(), Slug = $"t-{suffix}", Name = $"T{suffix}", Category = AccommodationCategory.HolidayHome },
                Zone = new Zone { Id = Guid.NewGuid(), Name = $"Z{suffix}", Category = AccommodationCategory.HolidayHome },
            });
            await context.SaveChangesAsync();
            return id;
        }

        private static Booking NewBooking(Guid unitId, DateOnly arrival, DateOnly departure) => new Booking
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
        };

        // Twee requests tegelijk, ieder met een eigen context en repository.
        private async Task<bool[]> BookAtTheSameTimeAsync(Booking first, Booking second)
        {
            await using ApplicationDBContext firstContext = Database.CreateContext();
            await using ApplicationDBContext secondContext = Database.CreateContext();

            return await Task.WhenAll(
                new BookingRepository(firstContext).TryAddAsync(first),
                new BookingRepository(secondContext).TryAddAsync(second));
        }
    }
}