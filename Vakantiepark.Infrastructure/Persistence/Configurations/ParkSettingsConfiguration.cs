using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vakantiepark.Domain.Entities;

namespace Vakantiepark.Infrastructure.Persistence.Configurations
{
    public static class ParkSettingsConfiguration
    {
        public static void Configure(this EntityTypeBuilder<ParkSettings> builder)
        {
            builder.Property(settings => settings.DepositPercentage).HasPrecision(5, 2);
        }
    }
}
