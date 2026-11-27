using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Vakantiepark.Domain.Entities;

namespace Vakantiepark.Infrastructure.Persistence.Configurations
{
    public static class ParkSettingsConfiguration
    {
        public static void Configure(this EntityTypeBuilder<ParkSettings> builder)
        {
            builder.Property(settings => settings.DepositPercentage).HasPrecision(5, 2);
            builder.Property(settings => settings.PetCleaningFee).HasPrecision(18, 2);
            builder.Property(settings => settings.SecurityDeposit).HasPrecision(18, 2);
            builder.Property(settings => settings.BankAccountNumber).IsRequired().HasMaxLength(34);
            builder.Property(settings => settings.BankAccountName).IsRequired().HasMaxLength(100);
        }
    }
}
