using Vakantiepark.Domain.Interfaces.Repositories;
using Vakantiepark.Infrastructure.Persistence;
using Vakantiepark.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Vakantiepark.Infrastructure.Repositories
{
    public class TrustedDeviceRepository : Repository<TrustedDevice>, ITrustedDeviceRepository
    {
        public TrustedDeviceRepository(ApplicationDBContext context)
            : base(context) { }

        public async Task<bool> IsTrustedAsync(Guid userId, string tokenHash, DateTime now)
            => await Set.AnyAsync(x => x.UserId == userId && x.TokenHash == tokenHash && x.ExpiresAt > now && x.DeletedAt == null);
    }
}
