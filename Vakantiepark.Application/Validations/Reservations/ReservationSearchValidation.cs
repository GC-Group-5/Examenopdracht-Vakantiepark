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

        // Empty means every status.
        public static BookingState? ParseStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return null;

            return status.Trim() switch
            {
                BookingStatuses.Bevestigd => BookingState.Confirmed,
                BookingStatuses.Afgerond => BookingState.Completed,
                BookingStatuses.Geannuleerd => BookingState.Cancelled,
                _ => throw new ArgumentException("Kies Bevestigd, Afgerond of Geannuleerd als status."),
            };
        }
    }
}
