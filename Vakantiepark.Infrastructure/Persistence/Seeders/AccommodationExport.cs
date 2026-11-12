using System.Text.Json;
using Vakantiepark.Domain.Entities;
using Vakantiepark.Domain.Enums;

namespace Vakantiepark.Infrastructure.Persistence.Seeders
{
    // Shape of the client's export file 'accommodaties.json'. Only used by the import.
    public sealed class AccommodationExport
    {
        public ExportCounts Counts { get; set; } = new();
        public List<ExportAccommodation> Accommodations { get; set; } = [];
    }

    public sealed class ExportCounts
    {
        public int HolidayHomes { get; set; }
        public int CampingPitches { get; set; }
        public int Total { get; set; }
    }

    public sealed class ExportAccommodation
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public AccommodationCategory Category { get; set; }
        public string Type { get; set; } = string.Empty;
        public int MaxPersons { get; set; }
        public string Zone { get; set; } = string.Empty;

        // Every property becomes a feature: true/false, a number or a text.
        public Dictionary<string, JsonElement> Properties { get; set; } = new();
    }

    // The entities built from the export, ready to be added to the context.
    public sealed record AccommodationImport(
        List<AccommodationType> Types,
        List<Zone> Zones,
        List<Feature> Features,
        List<Accommodation> Accommodations);
}
