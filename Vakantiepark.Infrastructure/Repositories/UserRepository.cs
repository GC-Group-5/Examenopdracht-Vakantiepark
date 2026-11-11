using Vakantiepark.Domain.Interfaces.Repositories;
using Vakantiepark.Infrastructure.Persistence;
using Vakantiepark.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Vakantiepark.Infrastructure.Repositories
{
    public class UserRepository : Repository<User>, IUserRepository
    {
        public UserRepository(ApplicationDBContext context)
            : base(context) { }

        // Emails are stored lowercased, so the caller passes a normalised address.
        public async Task<User?> GetByEmailAsync(string email)
            => await Set.SingleOrDefaultAsync(x => x.Email == email && x.DeletedAt == null);
    }
}
