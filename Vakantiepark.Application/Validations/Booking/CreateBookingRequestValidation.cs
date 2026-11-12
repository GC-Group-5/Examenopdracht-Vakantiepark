using Vakantiepark.Shared.DTOs.Booking;

namespace Vakantiepark.Application.Validations.Booking
{
    public static class CreateBookingRequestValidation
    {
        public const int MinimumMainBookerAge = 18;

        public const string NotAvailableMessage = "Dit verblijf is helaas niet meer beschikbaar in de gekozen periode. Kies andere data of een ander verblijf.";
        public const string NotAvailableInZoneMessage = "In de gekozen zone is dit verblijf niet meer beschikbaar in de gekozen periode. Kies een andere zone of geen voorkeur.";
        public const string BirthDateRequiredMessage = "Vul je geboortedatum in.";
        public const string MainBookerTooYoungMessage = "De hoofdboeker moet op de aankomstdatum minimaal 18 jaar zijn.";
        public const string TravelerCountMessage = "Het aantal reisgenoten klopt niet met het aantal personen van je zoekopdracht.";
        public const string TravelerDetailsMessage = "Vul van iedere reisgenoot de naam en geboortedatum in.";
        public const string BirthDateAfterArrivalMessage = "Een geboortedatum kan niet na de aankomstdatum liggen.";

        public static void Validate(this CreateBookingRequest request, List<Guid> availableUnitIds)
        {
            if (availableUnitIds.Count == 0)
                throw new ArgumentException(NotAvailableMessage);
        }

        // The main booker is one of NumberOfGuests; only the main booker has a minimum age.
        public static void ValidateParty(this CreateBookingRequest request)
        {
            if (request.BirthDate is not DateOnly birthDate)
                throw new ArgumentException(BirthDateRequiredMessage);

            if (birthDate.AddYears(MinimumMainBookerAge) > request.ArrivalDate)
                throw new ArgumentException(MainBookerTooYoungMessage);

            if (request.Travelers.Count != request.NumberOfGuests - 1)
                throw new ArgumentException(TravelerCountMessage);

            foreach (TravelerRequest traveler in request.Travelers)
            {
                if (string.IsNullOrWhiteSpace(traveler.Name) || traveler.BirthDate is null)
                    throw new ArgumentException(TravelerDetailsMessage);

                if (traveler.BirthDate > request.ArrivalDate)
                    throw new ArgumentException(BirthDateAfterArrivalMessage);
            }
        }
    }
}
