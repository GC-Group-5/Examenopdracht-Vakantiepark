namespace Vakantiepark.Application.Validations.Swimming
{
    public static class SwimDateValidation
    {
        public static void ValidateSwimDate(this DateOnly date, DateOnly arrivalDate, DateOnly departureDate, DateOnly parkToday)
        {
            if (date < arrivalDate || date > departureDate)
                throw new ArgumentException("Je kunt alleen zwemmomenten bekijken voor dagen tijdens je verblijf.");

            if (date < parkToday)
                throw new ArgumentException("Deze dag is al voorbij. Kies een andere dag.");
        }
    }
}
