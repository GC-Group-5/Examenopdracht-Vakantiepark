using Vakantiepark.Domain.Interfaces.Helpers;
using System.Security.Cryptography;

namespace Vakantiepark.Infrastructure.Helpers
{
    public class BookingNumberGenerator : IBookingNumberGenerator
    {
        // Leaves out 0, O, 1, I and L so guests can't misread the number.
        private const string Alphabet = "23456789ABCDEFGHJKMNPQRSTUVWXYZ";
        private const int Length = 8;

        public string Generate() => RandomNumberGenerator.GetString(Alphabet, Length);
    }
}
