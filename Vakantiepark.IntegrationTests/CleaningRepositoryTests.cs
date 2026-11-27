using Microsoft.EntityFrameworkCore;
using Vakantiepark.Domain.Entities;
using Vakantiepark.Domain.Enums;
using Vakantiepark.Infrastructure.Persistence;
using Vakantiepark.Infrastructure.Repositories;

namespace Vakantiepark.IntegrationTests
{
    // US-32: the cleanings of departures, against a real PostgreSQL. Every test uses its own unit.
    public class CleaningRepositoryTests : IClassFixture<PostgresFixture>
    {
        private static readonly DateOnly Today = new DateOnly(2027, 8, 2);

        private readonly PostgresFixture Database;

        public CleaningRepositoryTests(PostgresFixture database)
        {
            Database = database;
        }

        [Fact]
        public async Task GetByBookingIdsAsync_ReturnsOnlyTheseBookingsWithTheCleaner()
        {
            Accommodation unit = await CreateUnitAsync();
            User cleaner = await CreateCleanerAsync();
            Booking first = TestData.CreateBooking(unit.Id, Today.AddDays(-4), Today);
            Booking second = TestData.CreateBooking(unit.Id, Today.AddDays(3), Today.AddDays(6));
            await SaveAsync(first, second);
            await SaveAsync(CreateCleaning(first, unit, cleaner), CreateCleaning(second, unit, cleaner));

            await using ApplicationDBContext context = Database.CreateContext();
            List<Cleaning> cleanings = await new CleaningRepository(context).GetByBookingIdsAsync([first.Id]);

            Cleaning cleaning = Assert.Single(cleanings);
            Assert.Equal(first.Id, cleaning.BookingId);
            Assert.Equal(cleaner.FullName, cleaning.User?.FullName);
        }

        [Fact]
        public async Task SaveAsync_SecondCleaningForTheSameDeparture_IsRefused()
        {
            Accommodation unit = await CreateUnitAsync();
            User cleaner = await CreateCleanerAsync();
            Booking departure = TestData.CreateBooking(unit.Id, Today.AddDays(-4), Today);
            await SaveAsync(departure);
            await SaveAsync(CreateCleaning(departure, unit, cleaner));

            await Assert.ThrowsAsync<DbUpdateException>(() => SaveAsync(CreateCleaning(departure, unit, cleaner)));
        }

        [Fact]
        public async Task GetByBookingIdAsync_SavesStatusChanges()
        {
            Accommodation unit = await CreateUnitAsync();
            User cleaner = await CreateCleanerAsync();
            Booking departure = TestData.CreateBooking(unit.Id, Today.AddDays(-4), Today);
            await SaveAsync(departure);
            await SaveAsync(CreateCleaning(departure, unit, cleaner));

            await using (ApplicationDBContext context = Database.CreateContext())
            {
                CleaningRepository repository = new CleaningRepository(context);
                Cleaning cleaning = (await repository.GetByBookingIdAsync(departure.Id))!;
                cleaning.Status = CleaningStatus.Clean;
                cleaning.FinishedAt = DateTime.UtcNow;
                await repository.UpdateAsync(cleaning);
            }

            await using ApplicationDBContext readContext = Database.CreateContext();
            Cleaning? saved = await new CleaningRepository(readContext).GetByBookingIdAsync(departure.Id);
            Assert.Equal(CleaningStatus.Clean, saved?.Status);
            Assert.NotNull(saved?.FinishedAt);
        }

        private static Cleaning CreateCleaning(Booking booking, Accommodation unit, User cleaner)
            => new Cleaning()
            {
                Id = Guid.NewGuid(),
                BookingId = booking.Id,
                AccommodationUnitId = unit.Id,
                UserId = cleaner.Id,
                Status = CleaningStatus.InProgress,
                StartedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
            };

        private async Task SaveAsync(params Booking[] bookings)
        {
            await using ApplicationDBContext context = Database.CreateContext();
            context.Bookings.AddRange(bookings);
            await context.SaveChangesAsync();
        }

        private async Task SaveAsync(params Cleaning[] cleanings)
        {
            await using ApplicationDBContext context = Database.CreateContext();
            context.Cleanings.AddRange(cleanings);
            await context.SaveChangesAsync();
        }

        private async Task<Accommodation> CreateUnitAsync()
        {
            await using ApplicationDBContext context = Database.CreateContext();
            Accommodation unit = TestData.CreateUnit();
            context.Accommodations.Add(unit);
            await context.SaveChangesAsync();
            return unit;
        }

        private async Task<User> CreateCleanerAsync()
        {
            await using ApplicationDBContext context = Database.CreateContext();
            User cleaner = new User()
            {
                Id = Guid.NewGuid(),
                FullName = "Sam Schoonmaker",
                Email = $"schoonmaker.{Guid.NewGuid():N}@example.nl",
                PasswordHash = "hash",
                Role = UserRole.Schoonmaker,
                CreatedAt = DateTime.UtcNow,
            };
            context.Users.Add(cleaner);
            await context.SaveChangesAsync();
            return cleaner;
        }
    }
}
