using Vakantiepark.Domain.Enums;
using Vakantiepark.Domain.Interfaces;

namespace Vakantiepark.Domain.Entities
{
    public class Accommodation : IEntity
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public AccommodationCategory Category { get; set; }
        public string Zone { get; set; } = string.Empty;
        public int MaxPersons { get; set; }

        // real columns, per the US-02 acceptance criteria.
        public bool PetsAllowed { get; set; }
        public int MaxPets { get; set; }
        public bool WheelchairAccessible { get; set; }

        // Holiday home features: 0 and false for camping pitches.
        public int Bedrooms { get; set; }
        public int Bathrooms { get; set; }
        public int Beds { get; set; }
        public bool Sauna { get; set; }
        public bool Dishwasher { get; set; }
        public bool WashingMachine { get; set; }
        public bool AirConditioning { get; set; }
        public bool Fireplace { get; set; }
        public bool CoveredTerrace { get; set; }
        public bool ChargingPoint { get; set; }
        public bool Waterfront { get; set; }
        public bool NearPool { get; set; }
        public bool ChildFriendly { get; set; }
        public bool SmokeFree { get; set; }
        public bool HotTub { get; set; }
        public bool LevelAccess { get; set; }
        public bool AdaptedBathroom { get; set; }
        public bool WidenedDoors { get; set; }
        public bool AdjustableBed { get; set; }
        public bool ExtraParkingSpace { get; set; }

        // Camping pitch features: null and false for holiday homes.
        public bool Electricity { get; set; }
        public int ElectricityAmps { get; set; }
        public bool WaterConnection { get; set; }
        public bool DrainConnection { get; set; }
        public bool PrivateSanitary { get; set; }
        public bool CarAtPitch { get; set; }
        public bool NearSanitary { get; set; }
        public bool ExtraWide { get; set; }
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

        // Navigation Properties
        public AccommodationType Type { get; set; } = null!;
    }
}
