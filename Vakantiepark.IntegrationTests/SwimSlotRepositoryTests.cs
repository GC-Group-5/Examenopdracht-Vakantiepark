using Vakantiepark.Domain.Entities;
using Vakantiepark.Infrastructure.Persistence;
using Vakantiepark.Infrastructure.Repositories;

namespace Vakantiepark.IntegrationTests
{
    // US-22: the first guest to open a day stores its slots; two guests at the same moment must not store the day twice.
    public class SwimSlotRepositoryTests : IClassFixture<PostgresFixture>
    {
        private readonly PostgresFixture Database;

        public SwimSlotRepositoryTests(PostgresFixture database)
        {
            Database = database;
        }

        [Fact]
        public async Task TryAddRangeAsync_DayStoredByAnotherRequest_ReturnsFalseAndKeepsOneSetOfSlots()
        {
            DateOnly date = new DateOnly(2027, 7, 12);
            await using ApplicationDBContext first = Database.CreateContext();
            await using ApplicationDBContext second = Database.CreateContext();

            bool firstAdded = await new SwimSlotRepository(first).TryAddRangeAsync(CreateSlots(date));
            bool secondAdded = await new SwimSlotRepository(second).TryAddRangeAsync(CreateSlots(date));

            await using ApplicationDBContext context = Database.CreateContext();
            List<SwimSlot> stored = await new SwimSlotRepository(context).GetByDateAsync(date);
            Assert.True(firstAdded);
            Assert.False(secondAdded);
            Assert.Equal(2, stored.Count);
        }

        private static List<SwimSlot> CreateSlots(DateOnly date) =>
        [
            new SwimSlot { Id = Guid.NewGuid(), Date = date, StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(10, 30), Capacity = 80 },
            new SwimSlot { Id = Guid.NewGuid(), Date = date, StartTime = new TimeOnly(10, 30), EndTime = new TimeOnly(12, 0), Capacity = 80 },
        ];
    }
}
