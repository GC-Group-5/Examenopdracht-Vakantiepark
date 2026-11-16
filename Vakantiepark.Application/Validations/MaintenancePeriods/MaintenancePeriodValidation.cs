using Vakantiepark.Domain.Entities;
using Vakantiepark.Shared.DTOs.Maintenance;

namespace Vakantiepark.Application.Validations.MaintenancePeriods
{
    public static class MaintenancePeriodValidation
    {
        public static void ValidateMaintenancePeriod(this DateOnly startDate, DateOnly endDate)
        {
            DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
            if (startDate < today)
                throw new ArgumentException("De begindatum van een onderhoudsperiode mag niet in het verleden liggen.");

            if (endDate < startDate)
                throw new ArgumentException("De einddatum moet op of na de begindatum liggen.");
        }

        // Shortening or ending a period early (criterion 4). The period may already be running,
        // so its start may lie in the past; only the new end date has to be today or later.
        public static void ValidateShortening(this DateOnly startDate, DateOnly endDate, MaintenancePeriod existingPeriod)
        {
            if (startDate < existingPeriod.StartDate || endDate > existingPeriod.EndDate)
                throw new ArgumentException("Een periode kan alleen worden ingekort, niet verruimd.");

            if (startDate > existingPeriod.EndDate)
                throw new ArgumentException("De nieuwe begindatum valt na het einde van de periode.");

            DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
            if (endDate < today)
                throw new ArgumentException("Een periode kan niet in het verleden eindigen.");

            if (endDate < startDate)
                throw new ArgumentException("De einddatum moet op of na de begindatum liggen.");
        }
    }
}
