namespace Vakantiepark.Domain.Constants
{
    // Feature keys that business rules depend on. They match the property names in the
    // client's export, so a rename in the export fails the seeder tests instead of a search.
    public static class FeatureKeys
    {
        public const string MaxPets = "maxPets";
        public const string Bedrooms = "bedrooms";
        public const string WheelchairAccessible = "wheelchairAccessible";
        public const string ElectricityAmps = "electricityAmps";
        public const string MaxCaravanLengthMeters = "maxCaravanLengthMeters";
        public const string MaxVehicleLengthMeters = "maxVehicleLengthMeters";
    }
}
