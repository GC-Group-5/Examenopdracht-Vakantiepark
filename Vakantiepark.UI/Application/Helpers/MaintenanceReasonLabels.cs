namespace Vakantiepark.UI.Application.Helpers
{
    // Dutch text per maintenance reason (US-17), shared by the maintenance and the occupancy page.
    public static class MaintenanceReasonLabels
    {
        public static readonly Dictionary<string, string> All = new()
        {
            ["Maintenance"] = "Onderhoud",
            ["TechnicalProblem"] = "Technisch probleem",
            ["Other"] = "Overig",
        };

        public static string Get(string reason)
            => All.TryGetValue(reason, out string? label) ? label : reason;
    }
}
