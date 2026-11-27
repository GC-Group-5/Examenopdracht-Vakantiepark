using Vakantiepark.Shared.Constants;

namespace Vakantiepark.UI.Application.Helpers
{
    // US-32: badge colour per cleaning status, the same for the cleaner and for reception.
    public static class CleaningStatusBadge
    {
        public static string CssClass(string? status)
            => status switch
            {
                CleaningStatuses.Klaar => "text-bg-success",
                CleaningStatuses.Bezig => "text-bg-warning",
                _ => "text-bg-secondary",
            };
    }
}
