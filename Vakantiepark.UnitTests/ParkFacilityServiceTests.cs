using Moq;
using Vakantiepark.Application.Services;
using Vakantiepark.Domain.Entities;
using Vakantiepark.Domain.Enums;
using Vakantiepark.Domain.Interfaces.Repositories;
using Vakantiepark.Shared.DTOs.Park;
using Xunit;

namespace Vakantiepark.UnitTests
{
    public class ParkFacilityServiceTests
    {
        private readonly Mock<IRepository<ParkFacility>> _parkFacilityRepository = new Mock<IRepository<ParkFacility>>();

        // November: the park runs on CET, UTC+1.
        private ParkFacilityService CreateService(DateTime utcNow)
            => new ParkFacilityService(_parkFacilityRepository.Object, new FixedTimeProvider(new DateTimeOffset(utcNow, TimeSpan.Zero)));

        private void SetFacilities(params ParkFacility[] facilities)
            => _parkFacilityRepository.Setup(repository => repository.GetAllAsync()).ReturnsAsync(facilities);

        private static ParkFacility CreateShop() => new()
        {
            Name = "Parkwinkel",
            Category = ParkFacilityCategory.Supermarket,
            OpensAt = new TimeOnly(8, 0),
            ClosesAt = new TimeOnly(18, 0),
        };

        [Fact]
        public async Task GetAllAsync_ParkTimeWithinOpeningHours_IsOpen()
        {
            SetFacilities(CreateShop());

            List<ParkFacilityResponse> result = await CreateService(new DateTime(2026, 11, 23, 7, 30, 0)).GetAllAsync();

            Assert.True(Assert.Single(result).IsOpen);
        }

        [Fact]
        public async Task GetAllAsync_AfterClosingTime_IsClosed()
        {
            SetFacilities(CreateShop());

            List<ParkFacilityResponse> result = await CreateService(new DateTime(2026, 11, 23, 17, 0, 0)).GetAllAsync();

            Assert.False(Assert.Single(result).IsOpen);
        }

        [Fact]
        public async Task GetAllAsync_EndedActivity_IsLeftOut()
        {
            DateTime utcNow = new DateTime(2026, 11, 23, 12, 0, 0, DateTimeKind.Utc);
            SetFacilities(
                new ParkFacility { Name = "Bingoavond", Category = ParkFacilityCategory.Activity, StartsAt = utcNow.AddDays(-1), EndsAt = utcNow.AddDays(-1).AddHours(2) },
                new ParkFacility { Name = "Speurtocht", Category = ParkFacilityCategory.Activity, StartsAt = utcNow.AddHours(-1), EndsAt = utcNow.AddHours(1) });

            List<ParkFacilityResponse> result = await CreateService(utcNow).GetAllAsync();

            ParkFacilityResponse activity = Assert.Single(result);
            Assert.Equal("Speurtocht", activity.Name);
            Assert.True(activity.IsOpen);
        }
    }
}
