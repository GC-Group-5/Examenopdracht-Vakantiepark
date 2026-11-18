using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vakantiepark.Domain.Entities;

namespace Vakantiepark.Infrastructure.Persistence.Configurations
{
    public static class FeatureConfiguration
    {
        public static void Configure(this EntityTypeBuilder<Feature> builder)
        {
            builder.HasIndex(feature => feature.Key).IsUnique();
            builder.Property(feature => feature.Key).HasMaxLength(60).IsRequired();
            builder.Property(feature => feature.Name).HasMaxLength(80).IsRequired();
            builder.Property(feature => feature.ValueType).HasConversion<string>().HasMaxLength(20);
            builder.Property(feature => feature.Category).HasConversion<string>().HasMaxLength(20);
        }
    }
}
