using Vakantiepark.Shared.Constants;

namespace Vakantiepark.UI.Application.Helpers
{
    // Badge colour per booking status, the same on "Mijn boeking" and in the beheeromgeving.
    public static class BookingStatusBadge
    {
        public static string CssClass(string? status)
            => status switch
            {
                BookingStatuses.Geannuleerd or BookingStatuses.AanbetalingTeLaat => "text-bg-danger",
                BookingStatuses.InAfwachting => "text-bg-warning",
                BookingStatuses.Uitgecheckt => "text-bg-secondary",
                _ => "text-bg-success",
            };
    }
}
