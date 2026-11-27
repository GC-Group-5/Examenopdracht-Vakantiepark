using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Vakantiepark.Shared.DTOs.Park;
using Xunit;

namespace Vakantiepark.UnitTests
{
    // US-53 bug: the price limits were read in the current culture, so validating in the browser (nl-NL) crashed.
    public class UpdateTypePricesRequestTests
    {
        [Fact]
        public void Validate_DutchCulture_RejectsZeroPricesWithoutCrashing()
        {
            CultureInfo original = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = new CultureInfo("nl-NL");
            try
            {
                UpdateTypePricesRequest request = new UpdateTypePricesRequest { DefaultPricePerNight = 0m, CleaningFee = 60m };
                SeasonPriceRequest seasonPrice = new SeasonPriceRequest { SeasonId = Guid.NewGuid(), PricePerNight = 0m };

                Assert.Equal("De standaardprijs moet hoger dan 0 zijn.", Assert.Single(Validate(request)).ErrorMessage);
                Assert.Equal("Een seizoensprijs moet hoger dan 0 zijn.", Assert.Single(Validate(seasonPrice)).ErrorMessage);
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }

        private static List<ValidationResult> Validate(object model)
        {
            List<ValidationResult> results = [];
            Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
            return results;
        }
    }
}
