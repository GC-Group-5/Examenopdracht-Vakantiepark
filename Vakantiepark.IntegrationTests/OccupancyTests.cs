using Vakantiepark.Domain.Entities;
using Vakantiepark.Domain.Enums;
using Vakantiepark.Infrastructure.Persistence;
using Vakantiepark.Infrastructure.Repositories;

namespace Vakantiepark.IntegrationTests
{
    // US-16: which bookings and maintenance periods count on a date, against a real PostgreSQL.
    public class OccupancyTests : IClassFixture<PostgresFixture>
    {
        private static readonly DateOnly Date = new DateOnly(2027, 7, 14);

        private readonly PostgresFixture Database;

        public OccupancyTests(PostgresFixture database)
        {
            Database = database;
        }

        [Fact]
        public async Task GetStayingOnDateAsync_ArrivalDayCountsButDepartureDayAndCancelledDoNot()
        {
            Accommodation unit = await CreateUnitAsync();
            Booking leaves = TestData.CreateBooking(unit.Id, Date.AddDays(-4), Date);
            Booking arrives = TestData.CreateBooking(unit.Id, Date, Date.AddDays(3));
            Booking cancelled = TestData.CreateBooking(unit.Id, Date.AddDays(-1), Date.AddDays(2), booking => booking.DeletedAt = DateTime.UtcNow);
            await SaveAsync(leaves, arrives, cancelled);

            await using ApplicationDBContext context = Database.CreateContext();
            List<Booking> staying = await new BookingRepository(context).GetStayingOnDateAsync([unit.Id], Date);

            Assert.Equal(arrives.Id, Assert.Single(staying).Id);
        }

        [Fact]
        public async Task GetForOccupancyAsync_LoadsOnlyTheMaintenancePeriodThatCoversTheDate()
        {
            Accommodation unit = await CreateUnitAsync();
            MaintenancePeriod endsOnDate = NewPeriod(unit, Date.AddDays(-3), Date);
            await SaveAsync(endsOnDate, NewPeriod(unit, Date.AddDays(5), Date.AddDays(8)));

            await using ApplicationDBContext context = Database.CreateContext();
            AccommodationUnitRepository repository = new AccommodationUnitRepository(context);
            Accommodation onDate = Assert.Single(await repository.GetForOccupancyAsync(Date, unit.Type.Slug, null, null));
            Accommodation dayAfter = Assert.Single(await repository.GetForOccupancyAsync(Date.AddDays(1), unit.Type.Slug, null, null));

            Assert.Equal(endsOnDate.Id, Assert.Single(onDate.MaintenancePeriods).Id);
            Assert.Empty(dayAfter.MaintenancePeriods);
        }

        private async Task<Accommodation> CreateUnitAsync()
        {
            Accommodation unit = TestData.CreateUnit();
            await SaveAsync(unit);
            return unit;
        }

        private async Task SaveAsync(params object[] entities)
        {
            await using ApplicationDBContext context = Database.CreateContext();
            context.AddRange(entities);
            await context.SaveChangesAsync();
        }

        private static MaintenancePeriod NewPeriod(Accommodation unit, DateOnly startDate, DateOnly endDate) => new MaintenancePeriod
        {
            Id = Guid.NewGuid(),
            StartDate = startDate,
            EndDate = endDate,
            Reason = MaintenanceReason.Maintenance,
            AccommodationUnitId = unit.Id,
            CreatedAt = DateTime.UtcNow,
        };
    }
}
