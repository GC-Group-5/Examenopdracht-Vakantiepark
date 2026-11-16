using Vakantiepark.Domain.Constants;
using Vakantiepark.Domain.Entities;

namespace Vakantiepark.Domain.Filters
{
    // What the guest needs from a physical unit (US-06, US-36). Search and booking use this
    // same filter, so a unit the search offers is always a unit the booking may pick.
    public class AccommodationUnitFilter
    {
        public int NumberOfGuests { get; set; }
        public int NumberOfPets { get; set; }
        public bool WheelchairAccessible { get; set; }

        // Needs the unit's Features (with Feature) to be loaded.
        public bool Matches(Accommodation unit) =>
            unit.MaxPersons >= NumberOfGuests
            && (NumberOfPets == 0 || (unit.GetNumber(FeatureKeys.MaxPets) ?? 0) >= NumberOfPets)
            && (!WheelchairAccessible || unit.HasFeature(FeatureKeys.WheelchairAccessible));
    }
}
