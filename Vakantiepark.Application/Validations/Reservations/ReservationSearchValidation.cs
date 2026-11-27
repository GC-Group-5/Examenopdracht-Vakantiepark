using Vakantiepark.Application.Validations.Paging;
using Vakantiepark.Domain.Enums;
using Vakantiepark.Shared.Constants;
using Vakantiepark.Shared.DTOs.Reservations;

namespace Vakantiepark.Application.Validations.Reservations
{
    public static class ReservationSearchValidation
    {
        public static void Validate(this ReservationSearchRequest request)
        {
            request.ValidatePaging();

            if (request.From is DateOnly from && request.To is DateOnly to && to < from)
                throw new ArgumentException("De einddatum van de periode moet op of na de begindatum liggen.");
        }

        // "In afwachting" and "Aanbetaling te laat" are both AwaitingDeposit in the database; the due date
        // tells them apart. Null means the due date does not matter.
        public static bool? ParseDepositLate(string? status) => status?.Trim() switch
        {
            BookingStatuses.AanbetalingTeLaat => true,
            BookingStatuses.InAfwachting => false,
            _ => null,
        };

        // Empty means every status.
        public static BookingState? ParseStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return null;

            return status.Trim() switch
            {
                BookingStatuses.InAfwachting or BookingStatuses.AanbetalingTeLaat => BookingState.AwaitingDeposit,
                BookingStatuses.Bevestigd => BookingState.Confirmed,
                BookingStatuses.Ingecheckt => BookingState.CheckedIn,
                BookingStatuses.Uitgecheckt => BookingState.CheckedOut,
                BookingStatuses.Geannuleerd => BookingState.Cancelled,
                _ => throw new ArgumentException($"Kies {BookingStatuses.InAfwachting}, {BookingStatuses.AanbetalingTeLaat}, {BookingStatuses.Bevestigd}, {BookingStatuses.Ingecheckt}, {BookingStatuses.Uitgecheckt} of {BookingStatuses.Geannuleerd} als status."),
            };
        }
    }
}
