using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vakantiepark.Domain.Entities;

namespace Vakantiepark.Infrastructure.Persistence.Configurations
{
    public static class AccommodationTypePriceConfiguration
    {
        public static void Configure(this EntityTypeBuilder<AccommodationTypePrice> builder)
        {
            builder.HasIndex(price => new { price.AccommodationTypeId, price.SeasonId }).IsUnique();
            builder.Property(price => price.PricePerNight).HasPrecision(18, 2);

            builder.HasOne(price => price.AccommodationType)
                .WithMany(type => type.Prices)
                .HasForeignKey(price => price.AccommodationTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(price => price.Season)
                .WithMany(season => season.Prices)
                .HasForeignKey(price => price.SeasonId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
