using Vakantiepark.Application.Validations.Booking;
using Vakantiepark.Shared.DTOs.Booking;
using Xunit;

namespace Vakantiepark.UnitTests
{
    public class CreateBookingRequestValidationTests
    {
        private static readonly DateOnly Today = new DateOnly(2026, 11, 12);

        private static CreateBookingRequest Request(Action<CreateBookingRequest>? configure = null)
            => TestData.CreateNewBookingRequest(request =>
            {
                request.ArrivalDate = new DateOnly(2027, 4, 10);
                request.DepartureDate = new DateOnly(2027, 4, 15);
                configure?.Invoke(request);
            });

        [Fact]
        public void Validate_ValidRequest_DoesNotThrow()
        {
            Request().Validate(Today);
        }

        [Fact]
        public void Validate_NegativeNumberOfPets_ThrowsDutchMessage()
        {
            CreateBookingRequest request = Request(r => r.NumberOfPets = -1);

            ArgumentException exception = Assert.Throws<ArgumentException>(() => request.Validate(Today));

            Assert.Equal("Het aantal huisdieren kan niet negatief zijn.", exception.Message);
        }

        [Fact]
        public void Validate_MainBookerSeventeenOnArrival_ThrowsDutchMessage()
        {
            CreateBookingRequest request = Request(r => r.BirthDate = new DateOnly(2009, 4, 11));

            ArgumentException exception = Assert.Throws<ArgumentException>(() => request.Validate(Today));

            Assert.Equal("De hoofdboeker moet op de aankomstdatum minimaal 18 jaar zijn.", exception.Message);
        }

        [Fact]
        public void Validate_MainBookerTurnsEighteenOnArrivalDay_DoesNotThrow()
        {
            Request(r => r.BirthDate = new DateOnly(2009, 4, 10)).Validate(Today);
        }

        [Fact]
        public void Validate_NoBirthDate_Throws()
        {
            Assert.Throws<ArgumentException>(() => Request(r => r.BirthDate = null).Validate(Today));
        }

        [Fact]
        public void Validate_EveryoneButTheMainBookerUnder12_DoesNotThrow()
        {
            Request(r => r.NumberOfChildrenUnder12 = 3).Validate(Today);
        }

        [Fact]
        public void Validate_AsManyChildrenAsPersons_ThrowsDutchMessage()
        {
            CreateBookingRequest request = Request(r => r.NumberOfChildrenUnder12 = 4);

            ArgumentException exception = Assert.Throws<ArgumentException>(() => request.Validate(Today));

            Assert.Equal("Met 4 personen zijn er hoogstens 3 kinderen jonger dan 12: de hoofdboeker is 18 jaar of ouder.", exception.Message);
        }

        [Fact]
        public void Validate_NegativeNumberOfChildren_Throws()
        {
            Assert.Throws<ArgumentException>(() => Request(r => r.NumberOfChildrenUnder12 = -1).Validate(Today));
        }

        [Fact]
        public void Validate_ArrivalInThePast_Throws()
        {
            Assert.Throws<ArgumentException>(() => Request(r => r.ArrivalDate = Today.AddDays(-1)).Validate(Today));
        }

        [Fact]
        public void Validate_DepartureNotAfterArrival_Throws()
        {
            Assert.Throws<ArgumentException>(() => Request(r => r.DepartureDate = r.ArrivalDate).Validate(Today));
        }

        [Fact]
        public void Validate_OnePersonWithoutChildren_DoesNotThrow()
        {
            Request(r => { r.NumberOfGuests = 1; r.NumberOfChildrenUnder12 = 0; }).Validate(Today);
        }

        [Theory]
        [InlineData("2000-03-01", "2018-02-28", 17)]
        [InlineData("2000-03-01", "2018-03-01", 18)]
        [InlineData("2000-02-29", "2018-02-28", 17)]
        [InlineData("2000-02-29", "2018-03-01", 18)]
        public void AgeOn_CountsWholeYears(string birthDate, string date, int expectedAge)
        {
            Assert.Equal(expectedAge, CreateBookingRequestValidation.AgeOn(DateOnly.Parse(birthDate), DateOnly.Parse(date)));
        }
    }
}
