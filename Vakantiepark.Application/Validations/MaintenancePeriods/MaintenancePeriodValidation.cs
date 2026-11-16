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
    }
}
