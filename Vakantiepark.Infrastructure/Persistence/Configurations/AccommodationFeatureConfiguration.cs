using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vakantiepark.Domain.Entities;

namespace Vakantiepark.Infrastructure.Persistence.Configurations
{
    public static class AccommodationFeatureConfiguration
    {
        public static void Configure(this EntityTypeBuilder<AccommodationFeature> builder)
        {
            // One value per feature per accommodation.
            builder.HasKey(value => new { value.AccommodationId, value.FeatureId });
            builder.Property(value => value.NumberValue).HasPrecision(10, 2);

            builder.HasOne(value => value.Accommodation)
                .WithMany(accommodation => accommodation.Features)
                .HasForeignKey(value => value.AccommodationId)
                .OnDelete(DeleteBehavior.Cascade);

            // A feature or option that is still in use cannot be deleted.
            builder.HasOne(value => value.Feature)
                .WithMany(feature => feature.Accommodations)
                .HasForeignKey(value => value.FeatureId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(value => value.Option)
                .WithMany()
                .HasForeignKey(value => value.FeatureOptionId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
