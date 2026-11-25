namespace Vakantiepark.Shared.DTOs.Park
{
    // Everything the beheerder edits on the settings page, in the same shapes the page sends back.
    public class ParkSettingsResponse
    {
        public UpdateParkSettingsRequest Rules { get; set; } = new();
        public List<AddTouristTaxRateRequest> TouristTaxRates { get; set; } = [];
        public List<SeasonResponse> Seasons { get; set; } = [];
        public List<TypePricesResponse> Types { get; set; } = [];
    }
}
