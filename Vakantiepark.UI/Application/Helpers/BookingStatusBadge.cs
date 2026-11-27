using Vakantiepark.Shared.Constants;

namespace Vakantiepark.UI.Application.Helpers
{
    // Badge colour per booking status, the same on "Mijn boeking" and in the beheeromgeving.
    public static class BookingStatusBadge
    {
        public static string CssClass(string? status)
            => status switch
            {
                BookingStatuses.InAfwachtingVanAanbetaling => "text-bg-warning",
                BookingStatuses.Geannuleerd => "text-bg-danger",
                BookingStatuses.Afgerond => "text-bg-secondary",
                _ => "text-bg-success",
            };
    }
}
