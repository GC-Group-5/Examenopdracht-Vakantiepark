namespace Vakantiepark.Shared.DTOs.Booking
{
    public class CancellationQuoteResponse
    {
        public int DaysBeforeArrival { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal CancellationFee { get; set; }
        public string Explanation { get; set; } = string.Empty;
    }
}
