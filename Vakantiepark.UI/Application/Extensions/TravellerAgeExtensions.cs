namespace Vakantiepark.UI.Application.Extensions
{
    public static class TravellerAgeExtensions
    {
        private const int AdultAge = 18;

        public static string AgeGroupLabel(this DateOnly birthDate, DateOnly arrivalDate) =>
            AgeOn(birthDate, arrivalDate) >= AdultAge ? "Volwassene" : "Kind";

        public static int AgeOn(this DateOnly birthDate, DateOnly date)
        {
            int age = date.Year - birthDate.Year;
            if (birthDate > date.AddYears(-age))
                age--;
            return age;
        }
    }
}
