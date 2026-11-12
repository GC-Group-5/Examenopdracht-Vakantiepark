using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vakantiepark.Domain.Entities;

namespace Vakantiepark.Infrastructure.Persistence.Configurations
{
    public static class UserConfiguration
    {
        public static void Configure(this EntityTypeBuilder<User> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.FullName).IsRequired().HasMaxLength(100);
            builder.Property(x => x.Email).IsRequired().HasMaxLength(254);
            builder.Property(x => x.PasswordHash).IsRequired();
            builder.Property(x => x.Role).IsRequired().HasConversion<string>().HasMaxLength(20);

            builder.HasIndex(x => x.Email).IsUnique();
        }
    }
}
