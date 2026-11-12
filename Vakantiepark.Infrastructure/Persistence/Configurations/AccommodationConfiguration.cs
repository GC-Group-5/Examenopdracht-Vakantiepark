using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vakantiepark.Domain.Entities;
using Vakantiepark.Domain.Enums;

namespace Vakantiepark.Infrastructure.Persistence.Configurations
{
    public static class AccommodationConfiguration
    {
        public static void Configure(this EntityTypeBuilder<Accommodation> builder)
        {
            builder.HasIndex(accommodation => accommodation.Code).IsUnique();
            builder.Property(accommodation => accommodation.Code).HasMaxLength(16).IsRequired();
            builder.Property(accommodation => accommodation.Name).HasMaxLength(40).IsRequired();
            builder.Property(accommodation => accommodation.Zone).HasMaxLength(32).IsRequired();

            builder.HasOne(accommodation => accommodation.Type)
                .WithMany(type => type.Accommodations)
                .HasForeignKey(accommodation => accommodation.TypeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Descriptive yes/no features as data rows: direct many-to-many
            // through accommodation_features. Business-rule values (MaxPersons,
            // MaxPets, Bedrooms, meters...) stay real columns on the unit.
            builder.HasMany(accommodation => accommodation.Features)
                .WithMany(feature => feature.Accommodations)
                .UsingEntity(join => join.ToTable("accommodation_features"));

            builder.Property(accommodation => accommodation.Surface)
                .HasConversion(
                    surface => surface == PitchSurface.Grass ? "grass"
                        : surface == PitchSurface.Paved ? "paved"
                        : surface == PitchSurface.ReinforcedGrass ? "reinforced_grass"
                        : null,
                    value => value == "grass" ? PitchSurface.Grass
                        : value == "paved" ? PitchSurface.Paved
                        : value == "reinforced_grass" ? PitchSurface.ReinforcedGrass
                        : null,
                    valueComparer: new ValueComparer<PitchSurface?>(
                        (left, right) => left == right,
                        surface => surface.GetHashCode(),
                        surface => surface));

            builder.Property(accommodation => accommodation.Shade)
                .HasConversion(
                    shade => shade == PitchShade.None ? "none"
                        : shade == PitchShade.Low ? "low"
                        : shade == PitchShade.Medium ? "medium"
                        : shade == PitchShade.High ? "high"
                        : null,
                    value => value == "none" ? PitchShade.None
                        : value == "low" ? PitchShade.Low
                        : value == "medium" ? PitchShade.Medium
                        : value == "high" ? PitchShade.High
                        : null,
                    valueComparer: new ValueComparer<PitchShade?>(
                        (left, right) => left == right,
                        shade => shade.GetHashCode(),
                        shade => shade));

            builder.Property(accommodation => accommodation.WifiCoverage)
                .HasConversion(
                    wifi => wifi == WifiCoverage.Limited ? "limited"
                        : wifi == WifiCoverage.Standard ? "standard"
                        : wifi == WifiCoverage.Good ? "good"
                        : null,
                    value => value == "limited" ? WifiCoverage.Limited
                        : value == "standard" ? WifiCoverage.Standard
                        : value == "good" ? WifiCoverage.Good
                        : null,
                    valueComparer: new ValueComparer<WifiCoverage?>(
                        (left, right) => left == right,
                        wifi => wifi.GetHashCode(),
                        wifi => wifi));
        }
    }
}
