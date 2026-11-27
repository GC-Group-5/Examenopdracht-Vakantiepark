using Vakantiepark.Application.Extensions;
using Vakantiepark.Application.Interfaces;
using Vakantiepark.Domain.Entities;
using Vakantiepark.Domain.Interfaces.Repositories;
using Vakantiepark.Shared.DTOs.Park;

namespace Vakantiepark.Application.Services
{
    public class ParkFacilityService : IParkFacilityService
    {
        private readonly IRepository<ParkFacility> ParkFacilityRepository;
        private readonly TimeProvider TimeProvider;

        public ParkFacilityService(IRepository<ParkFacility> parkFacilityRepository, TimeProvider timeProvider)
        {
            ParkFacilityRepository = parkFacilityRepository;
            TimeProvider = timeProvider;
        }

        // Activities that have ended are left out.
        public async Task<List<ParkFacilityResponse>> GetAllAsync()
        {
            DateTimeOffset parkNow = TimeProvider.GetParkNow();
            IEnumerable<ParkFacility> facilities = await ParkFacilityRepository.GetAllAsync();

            return facilities
                .Where(facility => facility.EndsAt is null || facility.EndsAt > parkNow.UtcDateTime)
                .OrderBy(facility => facility.StartsAt)
                .ThenBy(facility => facility.Name)
                .Select(facility => facility.ToResponse(parkNow))
                .ToList();
        }
    }
}
