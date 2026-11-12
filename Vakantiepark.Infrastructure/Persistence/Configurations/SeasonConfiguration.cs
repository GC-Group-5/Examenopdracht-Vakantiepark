using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vakantiepark.Domain.Entities;

namespace Vakantiepark.Infrastructure.Persistence.Configurations
{
    public static class SeasonConfiguration
    {
        public static void Configure(this EntityTypeBuilder<Season> builder)
        {
            builder.HasIndex(season => season.Name).IsUnique();
            builder.HasIndex(season => new { season.StartDate, season.EndDate });
            builder.Property(season => season.Name).HasMaxLength(40).IsRequired();
        }
    }
}
