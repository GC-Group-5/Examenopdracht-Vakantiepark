using Vakantiepark.Domain.Entities;

namespace Vakantiepark.Domain.Interfaces.Repositories
{
    public interface ICleaningRepository : IRepository<Cleaning>
    {
        // With the cleaner loaded, so the overview can say who cleaned a unit.
        Task<List<Cleaning>> GetByBookingIdsAsync(List<Guid> bookingIds);
    }
}
