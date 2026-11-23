using Vakantiepark.Domain.Entities;
using Vakantiepark.Shared.DTOs.Park;

namespace Vakantiepark.Application.Extensions
{
    public static class ParkFacilityExtensions
    {
        // parkNow is in park time: DateTime gives the clock on the park, UtcDateTime compares with activities.
        public static bool IsOpenAt(this ParkFacility facility, DateTimeOffset parkNow)
        {
            if (facility.StartsAt is not null && facility.EndsAt is not null)
                return facility.StartsAt <= parkNow.UtcDateTime && parkNow.UtcDateTime < facility.EndsAt;

            if (facility.OpensAt is null || facility.ClosesAt is null)
                return false;

            TimeOnly time = TimeOnly.FromDateTime(parkNow.DateTime);
            return time >= facility.OpensAt && time < facility.ClosesAt;
        }

        public static ParkFacilityResponse ToResponse(this ParkFacility facility, DateTimeOffset parkNow) => new()
        {
            Name = facility.Name,
            Description = facility.Description,
            Category = facility.Category.ToString(),
            Location = facility.Location,
            PhotoUrl = facility.PhotoUrl,
            OpensAt = facility.OpensAt,
            ClosesAt = facility.ClosesAt,
            StartsAt = facility.StartsAt,
            EndsAt = facility.EndsAt,
            IsOpen = facility.IsOpenAt(parkNow),
        };
    }
}
