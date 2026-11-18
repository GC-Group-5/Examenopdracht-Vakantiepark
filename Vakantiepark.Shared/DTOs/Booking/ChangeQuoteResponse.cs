using Vakantiepark.Shared.DTOs.Price;

namespace Vakantiepark.Shared.DTOs.Booking
{
    public class ChangeQuoteResponse
    {
        public decimal OldTotalPrice { get; set; }
        public PriceSpecificationResponse NewPrice { get; set; } = new();

        public bool OtherUnit { get; set; }
    }
}
