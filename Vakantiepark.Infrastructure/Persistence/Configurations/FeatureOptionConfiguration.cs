using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vakantiepark.Domain.Entities;

namespace Vakantiepark.Infrastructure.Persistence.Configurations
{
    public static class FeatureOptionConfiguration
    {
        public static void Configure(this EntityTypeBuilder<FeatureOption> builder)
        {
            builder.HasIndex(option => new { option.FeatureId, option.Value }).IsUnique();
            builder.Property(option => option.Value).HasMaxLength(40).IsRequired();
            builder.Property(option => option.Name).HasMaxLength(60).IsRequired();

            builder.HasOne(option => option.Feature)
                .WithMany(feature => feature.Options)
                .HasForeignKey(option => option.FeatureId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
