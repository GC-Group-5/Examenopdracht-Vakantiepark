namespace Vakantiepark.Shared.DTOs.Booking
{
    // The code from the email link. A guest who logged in with booking number + email sends no
    // code: his guest login token says which booking is his.
    public class BookingAccessRequest
    {
        public string? Token { get; set; }
    }
}
