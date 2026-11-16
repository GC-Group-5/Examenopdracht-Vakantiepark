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
        public void Validate_FewerTravellersThanPersons_ThrowsDutchMessage()
        {
            CreateBookingRequest request = Request(r => r.CoTravellers.RemoveAt(0));

            ArgumentException exception = Assert.Throws<ArgumentException>(() => request.Validate(Today));

            Assert.Equal("Vul de naam en geboortedatum in van alle 4 personen.", exception.Message);
        }

        [Fact]
        public void Validate_TravellerWithoutName_Throws()
        {
            Assert.Throws<ArgumentException>(() => Request(r => r.CoTravellers[1].Name = " ").Validate(Today));
        }

        [Fact]
        public void Validate_TravellerBornInTheFuture_Throws()
        {
            Assert.Throws<ArgumentException>(() => Request(r => r.CoTravellers[2].BirthDate = Today.AddDays(1)).Validate(Today));
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
        public void Validate_OnePersonWithoutTravellers_DoesNotThrow()
        {
            Request(r => { r.NumberOfGuests = 1; r.CoTravellers.Clear(); }).Validate(Today);
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
