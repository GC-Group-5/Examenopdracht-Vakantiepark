namespace Vakantiepark.Shared.DTOs.Booking
{
    public class GuestResponse
    {
        public string Name { get; set; } = string.Empty;
        public DateOnly BirthDate { get; set; }
        public bool IsMainBooker { get; set; }
    }
}
