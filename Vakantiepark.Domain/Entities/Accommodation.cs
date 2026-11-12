using Vakantiepark.Domain.Enums;
using Vakantiepark.Domain.Interfaces;

namespace Vakantiepark.Domain.Entities
{
    public class Accommodation : IEntity
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Zone { get; set; } = string.Empty;

        // Business-rule values stay real columns (search, pets check, camper
        // length check). The descriptive yes/no flags (sauna, hot tub,
        // waterfront, car at pitch...) live in the Feature table via the
        // accommodation_features link, so a new flag is a data row.
        public int MaxPersons { get; set; }
        public int MaxPets { get; set; }

        // Holiday home values: null for camping pitches.
        public int? Bedrooms { get; set; }
        public int? Bathrooms { get; set; }
        public int? Beds { get; set; }

        // Camping pitch values: null for holiday homes.
        public int? ElectricityAmps { get; set; }

        // The one 'which kind' answers per pitch; not flags, so they stay columns.
        public PitchSurface? Surface { get; set; }
        public PitchShade? Shade { get; set; }
        public WifiCoverage? WifiCoverage { get; set; }

        // Meters with half-metre precision; null means the unit has no such limit.
        public double? MaxCaravanLengthMeters { get; set; }
        public double? MaxVehicleLengthMeters { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        // Navigation Keys
        public Guid TypeId { get; set; }

        // Navigation Properties: category lives on Type (HolidayHome/CampingPitch
        // inheritance is not needed). Features is a direct many-to-many.
        public AccommodationType Type { get; set; } = null!;
        public List<Feature> Features { get; set; } = [];
    }
}
