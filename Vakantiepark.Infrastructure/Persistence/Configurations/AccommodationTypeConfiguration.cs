using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vakantiepark.Domain.Entities;

namespace Vakantiepark.Infrastructure.Persistence.Configurations
{
    public static class AccommodationTypeConfiguration
    {
        public static void Configure(this EntityTypeBuilder<AccommodationType> builder)
        {
            builder.HasIndex(type => type.Slug).IsUnique();
            builder.HasIndex(type => type.Name).IsUnique();
            builder.Property(type => type.Slug).HasMaxLength(64).IsRequired();
            builder.Property(type => type.Name).HasMaxLength(32).IsRequired();
            builder.Property(type => type.Category).HasConversion<string>().HasMaxLength(20);
            builder.Property(type => type.DefaultPricePerNight).HasPrecision(18, 2);
            builder.Property(type => type.CleaningFee).HasPrecision(18, 2);
        }
    }
}
