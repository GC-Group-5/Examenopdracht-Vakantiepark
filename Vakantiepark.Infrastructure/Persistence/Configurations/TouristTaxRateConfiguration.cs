using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vakantiepark.Domain.Entities;

namespace Vakantiepark.Infrastructure.Persistence.Configurations
{
    public static class TouristTaxRateConfiguration
    {
        public static void Configure(this EntityTypeBuilder<TouristTaxRate> builder)
        {
            builder.HasIndex(rate => rate.ValidFrom).IsUnique();
            builder.Property(rate => rate.AmountPerPersonPerNight).HasPrecision(18, 2);
        }
    }
}
