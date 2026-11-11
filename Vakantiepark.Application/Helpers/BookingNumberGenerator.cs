using System.Security.Cryptography;

namespace Vakantiepark.Application.Helpers
{
    // Booking numbers are random instead of sequential, so a guest can't guess someone else's booking.
    public static class BookingNumberGenerator
    {
        public const string Prefix = "DOU-";
        public const int Length = 8;

        // No 0/O and 1/I, so a number read out over the phone is never ambiguous.
        private const string Characters = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

        public static string Create()
            => Prefix + RandomNumberGenerator.GetString(Characters, Length);
    }
}
