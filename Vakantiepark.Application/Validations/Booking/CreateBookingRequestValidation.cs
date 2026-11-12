using Vakantiepark.Shared.DTOs.Booking;

namespace Vakantiepark.Application.Validations.Booking
{
    public static class CreateBookingRequestValidation
    {
        public const string NotAvailableMessage = "Dit verblijf is helaas niet meer beschikbaar in de gekozen periode. Kies andere data of een ander verblijf.";

        public static void Validate(this CreateBookingRequest request, List<Guid> availableUnitIds)
        {
            if (availableUnitIds.Count == 0)
                throw new ArgumentException(NotAvailableMessage);
        }
    }
}
