using Vakantiepark.Domain.Entities;

namespace Vakantiepark.Domain.Interfaces.Repositories
{
    public interface ITrustedDeviceRepository : IRepository<TrustedDevice>
    {
        Task<bool> IsTrustedAsync(Guid userId, string tokenHash, DateTime now);
    }
}
