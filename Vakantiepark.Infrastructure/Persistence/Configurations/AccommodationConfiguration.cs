using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vakantiepark.Domain.Entities;

namespace Vakantiepark.Infrastructure.Persistence.Configurations
{
    public static class AccommodationConfiguration
    {
        public static void Configure(this EntityTypeBuilder<Accommodation> builder)
        {
            builder.HasIndex(accommodation => accommodation.Code).IsUnique();
            builder.Property(accommodation => accommodation.Code).HasMaxLength(16).IsRequired();
            builder.Property(accommodation => accommodation.Name).HasMaxLength(40).IsRequired();

            builder.HasOne(accommodation => accommodation.Type)
                .WithMany(type => type.Accommodations)
                .HasForeignKey(accommodation => accommodation.TypeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(accommodation => accommodation.Zone)
                .WithMany(zone => zone.Accommodations)
                .HasForeignKey(accommodation => accommodation.ZoneId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
