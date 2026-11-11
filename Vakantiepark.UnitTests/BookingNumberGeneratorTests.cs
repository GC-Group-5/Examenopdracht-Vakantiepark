using Vakantiepark.Application.Helpers;
using Xunit;

namespace Vakantiepark.UnitTests
{
    public class BookingNumberGeneratorTests
    {
        [Fact]
        public void Create_ReturnsPrefixAndEightUnambiguousCharacters()
        {
            string number = BookingNumberGenerator.Create();

            Assert.StartsWith(BookingNumberGenerator.Prefix, number);
            Assert.Equal(BookingNumberGenerator.Prefix.Length + BookingNumberGenerator.Length, number.Length);
            Assert.DoesNotContain(number[BookingNumberGenerator.Prefix.Length..], c => c is '0' or 'O' or '1' or 'I');
        }

        [Fact]
        public void Create_ManyNumbers_AreNotSequentialOrRepeated()
        {
            List<string> numbers = [.. Enumerable.Range(0, 1000).Select(_ => BookingNumberGenerator.Create())];

            Assert.Equal(numbers.Count, numbers.Distinct().Count());
        }
    }
}
