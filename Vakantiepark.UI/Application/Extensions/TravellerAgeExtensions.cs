namespace Vakantiepark.UI.Application.Extensions
{
    public static class TravellerAgeExtensions
    {
        // Same rule as the server: someone born on 29 February turns a year older on 1 March.
        public static int AgeOn(this DateOnly birthDate, DateOnly date)
        {
            int age = date.Year - birthDate.Year;
            if (birthDate > date.AddYears(-age))
                age--;
            return age;
        }
    }
}
