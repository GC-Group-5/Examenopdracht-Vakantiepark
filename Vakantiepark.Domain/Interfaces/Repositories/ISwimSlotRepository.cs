using Vakantiepark.Domain.Entities;

namespace Vakantiepark.Domain.Interfaces.Repositories
{
    public interface ISwimSlotRepository : IRepository<SwimSlot>
    {
        Task<List<SwimSlot>> GetByDateAsync(DateOnly date);

        // False when another request stored slots for the same day first.
        Task<bool> TryAddRangeAsync(List<SwimSlot> slots);
    }
}
