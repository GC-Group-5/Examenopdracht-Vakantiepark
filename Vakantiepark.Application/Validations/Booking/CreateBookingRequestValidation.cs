using Vakantiepark.Shared.Constants;
using Vakantiepark.Shared.DTOs.Booking;

namespace Vakantiepark.Application.Validations.Booking
{
    // Business rules for a new booking (US-08). Each rule throws an ArgumentException with a
    // message the guest can act on; the controller returns it as a 400.
    public static class CreateBookingRequestValidation
    {
        public const string NotAvailableMessage = "Dit verblijf is helaas niet meer beschikbaar in de gekozen periode. Kies andere data of een ander verblijf.";
        public const int MinimumMainBookerAge = BookingRules.MinimumMainBookerAge;

        // Checks that do not need the database.
        public static void ValidateStay(string accommodationTypeSlug, DateOnly arrivalDate, DateOnly departureDate, int numberOfGuests, int numberOfPets, DateOnly today)
        {
            if (string.IsNullOrWhiteSpace(accommodationTypeSlug))
                throw new ArgumentException("Kies een verblijf.");

            if (arrivalDate < today)
                throw new ArgumentException("De aankomstdatum ligt in het verleden.");

            if (departureDate <= arrivalDate)
                throw new ArgumentException("De vertrekdatum moet na de aankomstdatum liggen.");

            if (numberOfGuests < 1)
                throw new ArgumentException("Kies met hoeveel personen je komt.");

            if (numberOfPets < 0)
                throw new ArgumentException("Het aantal huisdieren kan niet negatief zijn.");
        }

        public static void Validate(this CreateBookingRequest request, DateOnly today)
        {
            ValidateStay(request.AccommodationTypeSlug, request.ArrivalDate, request.DepartureDate, request.NumberOfGuests, request.NumberOfPets, today);

            if (request.BirthDate is null)
                throw new ArgumentException("Vul je geboortedatum in.");

            if (AgeOn(request.BirthDate.Value, request.ArrivalDate) < MinimumMainBookerAge)
                throw new ArgumentException($"De hoofdboeker moet op de aankomstdatum minimaal {MinimumMainBookerAge} jaar zijn.");

            ValidateChildrenUnder12(request.NumberOfGuests, request.NumberOfChildrenUnder12);
            ValidateCoTravellers(request.NumberOfGuests, request.CoTravellers, today);
        }

        // Fellow travellers are optional; what is filled in must make sense. Also used when a guest changes his booking.
        public static void ValidateCoTravellers(int numberOfGuests, List<TravellerRequest> coTravellers, DateOnly today)
        {
            if (coTravellers.Count > numberOfGuests - 1)
                throw new ArgumentException($"Met {numberOfGuests} {(numberOfGuests == 1 ? "persoon" : "personen")} zijn er hoogstens {numberOfGuests - 1} reisgenoten.");

            foreach (TravellerRequest traveller in coTravellers)
            {
                if (traveller.BirthDate is null)
                    continue;

                if (string.IsNullOrWhiteSpace(traveller.Name))
                    throw new ArgumentException("Vul bij een ingevulde geboortedatum ook de naam van de reisgenoot in.");

                if (traveller.BirthDate.Value > today)
                    throw new ArgumentException($"De geboortedatum van {traveller.Name.Trim()} ligt in de toekomst.");
            }
        }

        // The main booker is 18 or older, so at most everyone else is under 12 (US-38).
        public static void ValidateChildrenUnder12(int numberOfGuests, int numberOfChildrenUnder12)
        {
            if (numberOfChildrenUnder12 < 0)
                throw new ArgumentException("Het aantal kinderen jonger dan 12 kan niet negatief zijn.");

            if (numberOfChildrenUnder12 >= numberOfGuests)
                throw new ArgumentException($"Met {numberOfGuests} {(numberOfGuests == 1 ? "persoon" : "personen")} zijn er hoogstens {numberOfGuests - 1} {(numberOfGuests - 1 == 1 ? "kind" : "kinderen")} jonger dan 12: de hoofdboeker is 18 jaar of ouder.");
        }

        // Age in whole years on a date; someone born on 29 February turns a year older on 1 March.
        public static int AgeOn(DateOnly birthDate, DateOnly date)
        {
            int age = date.Year - birthDate.Year;
            if (birthDate > date.AddYears(-age))
                age--;
            return age;
        }
    }
}
