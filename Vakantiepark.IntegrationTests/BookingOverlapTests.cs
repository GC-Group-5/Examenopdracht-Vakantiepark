using Vakantiepark.Domain.Entities;
using Vakantiepark.Infrastructure.Persistence;
using Vakantiepark.Infrastructure.Repositories;

namespace Vakantiepark.IntegrationTests
{
    // US-45: one overlap query for booking (nothing left out) and for changing (the own booking left out).
    public class BookingOverlapTests : IClassFixture<PostgresFixture>
    {
        private static readonly DateOnly Arrival = new DateOnly(2027, 7, 10);
        private static readonly DateOnly Departure = new DateOnly(2027, 7, 17);

        private readonly PostgresFixture Database;

        public BookingOverlapTests(PostgresFixture database)
        {
            Database = database;
        }

        [Fact]
        public async Task GetUnitIdsWithOverlappingBookingAsync_WithoutExcludedBooking_ReturnsTheBookedUnit()
        {
            (Accommodation unit, Booking _) = await CreateBookedUnitAsync();

            await using ApplicationDBContext context = Database.CreateContext();
            List<Guid> booked = await new BookingRepository(context).GetUnitIdsWithOverlappingBookingAsync([unit.Id], Arrival.AddDays(2), Departure.AddDays(2));

            Assert.Equal(unit.Id, Assert.Single(booked));
        }

        [Fact]
        public async Task GetUnitIdsWithOverlappingBookingAsync_OwnBookingExcluded_UnitCountsAsFree()
        {
            (Accommodation unit, Booking booking) = await CreateBookedUnitAsync();

            await using ApplicationDBContext context = Database.CreateContext();
            List<Guid> booked = await new BookingRepository(context).GetUnitIdsWithOverlappingBookingAsync([unit.Id], Arrival.AddDays(2), Departure.AddDays(2), booking.Id);

            Assert.Empty(booked);
        }

        private async Task<(Accommodation Unit, Booking Booking)> CreateBookedUnitAsync()
        {
            Accommodation unit = TestData.CreateUnit();
            Booking booking = TestData.CreateBooking(unit.Id, Arrival, Departure);

            await using ApplicationDBContext context = Database.CreateContext();
            context.AddRange(unit, booking);
            await context.SaveChangesAsync();

            return (unit, booking);
        }
    }
}
