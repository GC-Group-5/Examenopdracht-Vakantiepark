using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vakantiepark.Domain.Entities;

namespace Vakantiepark.Infrastructure.Persistence.Configurations
{
    public static class ParkFacilityConfiguration
    {
        public static void Configure(this EntityTypeBuilder<ParkFacility> builder)
        {
            builder.Property(facility => facility.Name).HasMaxLength(80).IsRequired();
            builder.Property(facility => facility.Description).HasMaxLength(500).IsRequired();
            builder.Property(facility => facility.Category).HasConversion<string>().HasMaxLength(20);
            builder.Property(facility => facility.Location).HasMaxLength(80).IsRequired();
            builder.Property(facility => facility.PhotoUrl).HasMaxLength(200).IsRequired();
        }
    }
}
