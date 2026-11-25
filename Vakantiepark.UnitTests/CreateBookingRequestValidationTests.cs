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
        public void ValidatePetCount_AboveAccommodationMaximum_ThrowsDutchMessage()
        {
            ArgumentException exception = Assert.Throws<ArgumentException>(
                () => CreateBookingRequestValidation.ValidatePetCount(3, 2));

            Assert.Equal("Dit verblijfstype staat maximaal 2 huisdieren toe.", exception.Message);
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
        public void Validate_EmptyTravellers_DoesNotThrow()
        {
            Request(r => r.CoTravellers = [new TravellerRequest(), new TravellerRequest() { Name = "Els Jansen" }]).Validate(Today);
        }

        [Fact]
        public void Validate_TravellerBirthDateWithoutName_Throws()
        {
            CreateBookingRequest request = Request(r => r.CoTravellers = [new TravellerRequest() { BirthDate = new DateOnly(2015, 1, 1) }]);

            Assert.Throws<ArgumentException>(() => request.Validate(Today));
        }

        [Fact]
        public void Validate_TravellerBornInTheFuture_Throws()
        {
            CreateBookingRequest request = Request(r => r.CoTravellers = [new TravellerRequest() { Name = "Els Jansen", BirthDate = Today.AddDays(1) }]);

            Assert.Throws<ArgumentException>(() => request.Validate(Today));
        }

        [Fact]
        public void Validate_MoreTravellersThanPersons_Throws()
        {
            CreateBookingRequest request = Request(r =>
            {
                r.NumberOfGuests = 2;
                r.CoTravellers = [new TravellerRequest() { Name = "Els" }, new TravellerRequest() { Name = "Piet" }];
            });

            Assert.Throws<ArgumentException>(() => request.Validate(Today));
        }

        [Fact]
        public void Validate_NoBirthDate_Throws()
        {
            Assert.Throws<ArgumentException>(() => Request(r => r.BirthDate = null).Validate(Today));
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
        public void Validate_OnePerson_DoesNotThrow()
        {
            Request(r => r.NumberOfGuests = 1).Validate(Today);
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
