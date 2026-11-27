using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vakantiepark.Domain.Entities;

namespace Vakantiepark.Infrastructure.Persistence.Configurations
{
    public static class SwimSlotConfiguration
    {
        public static void Configure(this EntityTypeBuilder<SwimSlot> builder)
        {
            builder.HasKey(x => x.Id);

            // Two requests for a new day at the same time must not create the day twice.
            builder.HasIndex(x => new { x.Date, x.StartTime })
                .IsUnique()
                .HasFilter("deleted_at IS NULL");
        }
    }
}
