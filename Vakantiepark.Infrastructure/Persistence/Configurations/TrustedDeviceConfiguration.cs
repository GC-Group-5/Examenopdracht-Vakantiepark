using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vakantiepark.Domain.Entities;

namespace Vakantiepark.Infrastructure.Persistence.Configurations
{
    public static class TrustedDeviceConfiguration
    {
        public static void Configure(this EntityTypeBuilder<TrustedDevice> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.TokenHash).IsRequired().HasMaxLength(64);

            builder.HasIndex(x => x.TokenHash).IsUnique();

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
