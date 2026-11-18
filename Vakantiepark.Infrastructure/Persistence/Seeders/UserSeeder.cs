using Vakantiepark.Infrastructure.Helpers;
using Vakantiepark.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Vakantiepark.Domain.Enums;

namespace Vakantiepark.Infrastructure.Persistence.Seeders
{
    // Test accounts for development only (one per role). The passwords are listed in README.md.
    public static class UserSeeder
    {
        private static readonly (string FullName, string Email, string Password, UserRole Role)[] TestUsers =
        [
            ("Mila Medewerker", "medewerker@dropuut.example", "Medewerker123!", UserRole.Medewerker),
            ("Bas Beheerder", "beheerder@dropuut.example", "Beheerder123!", UserRole.Beheerder),
            ("Salina Schoonmaker", "schoonmaker@dropuut.example", "Schoonmaker123!", UserRole.Schoonmaker),
            ("Gerrit Gast", "gast@dropuut.example", "Gast123!", UserRole.Gast),
        ];

        // Adds each account whose email is missing, so a database from before a new role still gets it.
        public static void SeedTestUsers(this DbContext context)
        {
            DbSet<User> users = context.Set<User>();
            List<string> existingEmails = users.Select(user => user.Email).ToList();

            PasswordHasher passwordHasher = new PasswordHasher();
            DateTime now = DateTime.UtcNow;

            List<User> missing = TestUsers
                .Where(testUser => !existingEmails.Contains(testUser.Email))
                .Select(testUser => new User()
                {
                    FullName = testUser.FullName,
                    Email = testUser.Email,
                    PasswordHash = passwordHasher.Hash(testUser.Password),
                    Role = testUser.Role,
                    CreatedAt = now
                })
                .ToList();

            if (missing.Count == 0)
                return;

            users.AddRange(missing);
            context.SaveChanges();
        }
    }
}
