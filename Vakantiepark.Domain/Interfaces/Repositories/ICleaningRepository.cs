using Vakantiepark.Domain.Entities;

namespace Vakantiepark.Domain.Interfaces.Repositories
{
    public interface ICleaningRepository : IRepository<Cleaning>
    {
        // With the cleaner loaded, so the overview can say who cleaned a unit.
        Task<List<Cleaning>> GetByBookingIdsAsync(List<Guid> bookingIds);

        // Tracked and with the cleaner, so starting or finishing can say who is already busy.
        Task<Cleaning?> GetByBookingIdAsync(Guid bookingId);
    }
}
