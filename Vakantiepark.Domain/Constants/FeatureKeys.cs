namespace Vakantiepark.Domain.Constants
{
    // Feature keys that business rules depend on. They match the property names in the
    // client's export, so a rename in the export fails the seeder tests instead of a search.
    public static class FeatureKeys
    {
        public const string MaxPets = "maxPets";
        public const string Bedrooms = "bedrooms";
        public const string Bathrooms = "bathrooms";
        public const string WheelchairAccessible = "wheelchairAccessible";
        public const string Sauna = "sauna";
        public const string HotTub = "hotTub";
        public const string Dishwasher = "dishwasher";
        public const string WashingMachine = "washingMachine";
        public const string AirConditioning = "airConditioning";
        public const string Fireplace = "fireplace";
        public const string CoveredTerrace = "coveredTerrace";
        public const string ElectricityAmps = "electricityAmps";
        public const string WaterConnection = "waterConnection";
        public const string DrainConnection = "drainConnection";
        public const string PrivateSanitary = "privateSanitary";
        public const string MaxCaravanLengthMeters = "maxCaravanLengthMeters";
        public const string MaxVehicleLengthMeters = "maxVehicleLengthMeters";
    }
}
