using Vakantiepark.Infrastructure.Helpers;
using Vakantiepark.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Vakantiepark.Domain.Enums;

namespace Vakantiepark.Infrastructure.Persistence.Seeders
{
    // Test accounts for development only (one per staff role). The passwords are listed in README.md.
    public static class UserSeeder
    {
        public static void SeedTestUsers(this DbContext context)
        {
            DbSet<User> users = context.Set<User>();
            if (users.Any())
                return;

            PasswordHasher passwordHasher = new PasswordHasher();
            DateTime now = DateTime.UtcNow;

            users.AddRange(
                new User()
                {
                    FullName = "Mila Medewerker",
                    Email = "medewerker@dropuut.example",
                    PasswordHash = passwordHasher.Hash("Medewerker123!"),
                    Role = UserRole.Medewerker,
                    CreatedAt = now
                },
                new User()
                {
                    FullName = "Bas Beheerder",
                    Email = "beheerder@dropuut.example",
                    PasswordHash = passwordHasher.Hash("Beheerder123!"),
                    Role = UserRole.Beheerder,
                    CreatedAt = now
                });

            context.SaveChanges();
        }
    }
}
