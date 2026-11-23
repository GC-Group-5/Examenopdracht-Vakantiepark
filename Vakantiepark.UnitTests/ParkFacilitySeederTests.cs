using Vakantiepark.Domain.Entities;
using Vakantiepark.Infrastructure.Persistence.Seeders;
using Xunit;

namespace Vakantiepark.UnitTests
{
    // Review of #86: activity times are Dutch times, so the UTC value shifts with daylight saving time.
    public class ParkFacilitySeederTests
    {
        [Fact]
        public void BuildActivities_Winter_TenOClockParkTimeIsNineUtc()
        {
            List<ParkFacility> activities = ParkFacilitySeeder.BuildActivities(new DateTime(2026, 11, 23, 12, 0, 0, DateTimeKind.Utc));

            Assert.Equal(new DateTime(2026, 11, 24, 9, 0, 0, DateTimeKind.Utc), activities[0].StartsAt);
        }

        [Fact]
        public void BuildActivities_Summer_TenOClockParkTimeIsEightUtc()
        {
            List<ParkFacility> activities = ParkFacilitySeeder.BuildActivities(new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc));

            Assert.Equal(new DateTime(2026, 7, 2, 8, 0, 0, DateTimeKind.Utc), activities[0].StartsAt);
        }

        [Fact]
        public void BuildActivities_AllStartAfterNowAsUtc()
        {
            DateTime now = new DateTime(2026, 11, 23, 23, 30, 0, DateTimeKind.Utc);

            List<ParkFacility> activities = ParkFacilitySeeder.BuildActivities(now);

            Assert.All(activities, activity =>
            {
                Assert.True(activity.StartsAt > now);
                Assert.Equal(DateTimeKind.Utc, activity.StartsAt!.Value.Kind);
            });
        }
    }
}
