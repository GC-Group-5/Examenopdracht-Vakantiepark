using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vakantiepark.Domain.Entities;

namespace Vakantiepark.Infrastructure.Persistence.Configurations
{
    public static class ZoneConfiguration
    {
        public static void Configure(this EntityTypeBuilder<Zone> builder)
        {
            // The same name may exist once for the homes and once for the camping.
            builder.HasIndex(zone => new { zone.Name, zone.Category }).IsUnique();
            builder.Property(zone => zone.Name).HasMaxLength(40).IsRequired();
            builder.Property(zone => zone.Category).HasConversion<string>().HasMaxLength(20);
        }
    }
}
