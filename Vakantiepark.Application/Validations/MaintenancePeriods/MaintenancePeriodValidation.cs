using Vakantiepark.Domain.Entities;

namespace Vakantiepark.Application.Validations.MaintenancePeriods
{
    public static class MaintenancePeriodValidation
    {
        public static void ValidateMaintenancePeriod(this DateOnly startDate, DateOnly endDate, DateOnly today)
        {
            if (startDate < today)
                throw new ArgumentException("De begindatum van een onderhoudsperiode mag niet in het verleden liggen.");

            if (endDate < startDate)
                throw new ArgumentException("De einddatum moet op of na de begindatum liggen.");
        }

        // Shortening, ending early or extending a period (criterion 4; extending was added so staff
        // can take more time). A running period keeps its start date, and no period ends in the past.
        public static void ValidatePeriodChange(this DateOnly startDate, DateOnly endDate, MaintenancePeriod existingPeriod, DateOnly today)
        {
            if (existingPeriod.EndDate < today)
                throw new ArgumentException("Een afgelopen periode kan niet meer worden aangepast. Leg een nieuwe periode vast.");

            bool alreadyStarted = existingPeriod.StartDate <= today;
            if (alreadyStarted && startDate != existingPeriod.StartDate)
                throw new ArgumentException("De begindatum van een lopende periode kan niet meer worden aangepast.");

            if (!alreadyStarted && startDate < today)
                throw new ArgumentException("De begindatum van een onderhoudsperiode mag niet in het verleden liggen.");

            if (endDate < today)
                throw new ArgumentException("Een periode kan niet in het verleden eindigen.");

            if (endDate < startDate)
                throw new ArgumentException("De einddatum moet op of na de begindatum liggen.");
        }

        // One unit never has two overlapping periods: staff changes the existing one instead.
        public static void ValidateNoOverlap(this IEnumerable<MaintenancePeriod> otherPeriods, DateOnly startDate, DateOnly endDate)
        {
            MaintenancePeriod? overlapping = otherPeriods.FirstOrDefault(period => period.StartDate <= endDate && period.EndDate >= startDate);
            if (overlapping is not null)
                throw new ArgumentException($"Deze accommodatie heeft al een onderhoudsperiode van {overlapping.StartDate:dd-MM-yyyy} t/m {overlapping.EndDate:dd-MM-yyyy}. Periodes van één accommodatie mogen niet overlappen; pas die periode aan of kies andere data.");
        }

        public static void ValidateCanEnd(this MaintenancePeriod period, DateOnly today)
        {
            if (period.EndDate < today)
                throw new ArgumentException("Deze periode is al afgelopen.");
        }
    }
}
