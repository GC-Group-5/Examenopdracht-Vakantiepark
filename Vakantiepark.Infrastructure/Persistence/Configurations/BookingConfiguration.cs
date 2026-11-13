using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vakantiepark.Domain.Entities;

namespace Vakantiepark.Infrastructure.Persistence.Configurations
{
    public static class BookingConfiguration
    {
        public static void Configure(this EntityTypeBuilder<Booking> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.BookingNumber).IsRequired().HasMaxLength(20);
            builder.HasIndex(x => x.BookingNumber).IsUnique();

            builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
            builder.Property(x => x.Email).IsRequired().HasMaxLength(254);
            builder.Property(x => x.PhoneNumber).IsRequired().HasMaxLength(30);

            builder.Property(x => x.AccommodationPrice).HasPrecision(18, 2);
            builder.Property(x => x.CleaningFee).HasPrecision(18, 2);
            builder.Property(x => x.TouristTax).HasPrecision(18, 2);
            builder.Property(x => x.TotalPrice).HasPrecision(18, 2);
            builder.Property(x => x.Deposit).HasPrecision(18, 2);

            builder.HasOne(x => x.AccommodationUnit)
                .WithMany()
                .HasForeignKey(x => x.AccommodationUnitId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.Guests)
                .WithOne(x => x.Booking)
                .HasForeignKey(x => x.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            // Overlapping bookings of one unit are blocked by the exclusion constraint
            // in migration AddBookingOverlapConstraint; EF cannot model that constraint.
        }
    }
}
