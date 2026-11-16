using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vakantiepark.Domain.Entities;

namespace Vakantiepark.Infrastructure.Persistence.Configurations
{
    public static class MaintenancePeriodConfiguration
    {
        public static void Configure(this EntityTypeBuilder<MaintenancePeriod> builder)
        {
            builder.HasIndex(period => new { period.AccommodationUnitId, period.StartDate, period.EndDate });

            builder.Property(period => period.Reason).HasConversion<string>().HasMaxLength(20);
            builder.Property(period => period.Note).HasMaxLength(300);

            builder.HasOne(period => period.AccommodationUnit)
                .WithMany(unit => unit.MaintenancePeriods)
                .HasForeignKey(period => period.AccommodationUnitId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
