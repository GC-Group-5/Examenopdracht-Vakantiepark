using Vakantiepark.Domain.Entities;
using Vakantiepark.Domain.Enums;
using Vakantiepark.Shared.Constants;
using Vakantiepark.Shared.DTOs.Cleaning;

namespace Vakantiepark.Application.Extensions
{
    public static class CleaningExtensions
    {
        // Needs AccommodationUnit with Type and Zone loaded. Takes nothing personal from the booking (US-32).
        public static CleaningItemResponse ToCleaningItem(this Booking departure, Cleaning? cleaning, bool guestsArriveToday)
            => new CleaningItemResponse()
            {
                BookingId = departure.Id,
                UnitCode = departure.AccommodationUnit.Code,
                TypeName = departure.AccommodationUnit.Type.Name,
                Zone = departure.AccommodationUnit.Zone.Name,
                GuestsArriveToday = guestsArriveToday,
                Status = (cleaning?.Status ?? CleaningStatus.ToClean).ToText(),
                CleanedBy = cleaning?.User?.FullName,
                FinishedAt = cleaning?.FinishedAt,
            };

        public static string ToText(this CleaningStatus status) => status switch
        {
            CleaningStatus.InProgress => CleaningStatuses.Bezig,
            CleaningStatus.Clean => CleaningStatuses.Klaar,
            _ => CleaningStatuses.TeSchoonmaken,
        };
    }
}
