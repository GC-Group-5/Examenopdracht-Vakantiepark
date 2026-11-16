using Vakantiepark.Shared.DTOs.Booking;

namespace Vakantiepark.Application.Validations.Booking
{
    // Business rules for a new booking (US-08). Each rule throws an ArgumentException with a
    // message the guest can act on; the controller returns it as a 400.
    public static class CreateBookingRequestValidation
    {
        public const string NotAvailableMessage = "Dit verblijf is helaas niet meer beschikbaar in de gekozen periode. Kies andere data of een ander verblijf.";
        public const int MinimumMainBookerAge = 18;

        // Checks that do not need the database.
        public static void ValidateStay(string accommodationTypeSlug, DateOnly arrivalDate, DateOnly departureDate, int numberOfGuests, DateOnly today)
        {
            if (string.IsNullOrWhiteSpace(accommodationTypeSlug))
                throw new ArgumentException("Kies een verblijf.");

            if (arrivalDate < today)
                throw new ArgumentException("De aankomstdatum ligt in het verleden.");

            if (departureDate <= arrivalDate)
                throw new ArgumentException("De vertrekdatum moet na de aankomstdatum liggen.");

            if (numberOfGuests < 1)
                throw new ArgumentException("Kies met hoeveel personen je komt.");
        }

        public static void Validate(this CreateBookingRequest request, DateOnly today)
        {
            ValidateStay(request.AccommodationTypeSlug, request.ArrivalDate, request.DepartureDate, request.NumberOfGuests, today);

            if (request.BirthDate is null)
                throw new ArgumentException("Vul je geboortedatum in.");

            if (AgeOn(request.BirthDate.Value, request.ArrivalDate) < MinimumMainBookerAge)
                throw new ArgumentException($"De hoofdboeker moet op de aankomstdatum minimaal {MinimumMainBookerAge} jaar zijn.");

            int coTravellers = request.NumberOfGuests - 1;
            if (request.CoTravellers.Count != coTravellers)
                throw new ArgumentException($"Vul de naam en geboortedatum in van alle {request.NumberOfGuests} personen.");

            foreach (TravellerRequest traveller in request.CoTravellers)
            {
                if (string.IsNullOrWhiteSpace(traveller.Name))
                    throw new ArgumentException("Vul de naam van iedere reisgenoot in.");

                if (traveller.BirthDate is null)
                    throw new ArgumentException($"Vul de geboortedatum van {traveller.Name.Trim()} in.");

                if (traveller.BirthDate.Value > today)
                    throw new ArgumentException($"De geboortedatum van {traveller.Name.Trim()} ligt in de toekomst.");
            }
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
