using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vakantiepark.Domain.Entities;

namespace Vakantiepark.Infrastructure.Persistence.Configurations
{
    public static class CleaningConfiguration
    {
        public static void Configure(this EntityTypeBuilder<Cleaning> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);

            // One cleaning per departure.
            builder.HasIndex(x => x.BookingId).IsUnique();

            // The history is read per unit, newest first.
            builder.HasIndex(x => new { x.AccommodationUnitId, x.FinishedAt });

            builder.HasOne(x => x.Booking)
                .WithMany()
                .HasForeignKey(x => x.BookingId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.AccommodationUnit)
                .WithMany()
                .HasForeignKey(x => x.AccommodationUnitId)
                .OnDelete(DeleteBehavior.Restrict);

            // A cleaner's account is never deleted while his cleanings are in the history.
            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
