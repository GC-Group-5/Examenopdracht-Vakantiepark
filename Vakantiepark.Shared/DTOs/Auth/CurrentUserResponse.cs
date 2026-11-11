namespace Vakantiepark.Shared.DTOs.Auth
{
    public class CurrentUserResponse
    {
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? BookingNumber { get; set; }
    }
}
