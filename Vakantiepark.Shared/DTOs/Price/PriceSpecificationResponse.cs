namespace Vakantiepark.Shared.DTOs.Price
{
    public class PriceSpecificationResponse
    {
        public decimal AccommodationPrice { get; set; }
        public decimal CleaningFee { get; set; }
        public decimal TouristTax { get; set; }
        public decimal TotalPrice { get; set; }

        // Part of the total paid at booking (ParkSettings.DepositPercentage).
        public decimal Deposit { get; set; }
    }
}
