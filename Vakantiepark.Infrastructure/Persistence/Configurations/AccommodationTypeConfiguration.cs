using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vakantiepark.Domain.Entities;
using Vakantiepark.Domain.Enums;

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

            builder.Property(type => type.Category)
                .HasConversion(
                    category => category == AccommodationCategory.CampingPitch ? "camping_pitch" : "holiday_home",
                    value => value == "camping_pitch" ? AccommodationCategory.CampingPitch : AccommodationCategory.HolidayHome,
                    valueComparer: new ValueComparer<AccommodationCategory>(
                        (left, right) => left == right,
                        category => category.GetHashCode(),
                        category => category));
        }
    }
}
