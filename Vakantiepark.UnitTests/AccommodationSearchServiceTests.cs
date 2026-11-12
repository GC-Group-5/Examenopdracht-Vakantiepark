using Moq;
using Vakantiepark.Application.Interfaces;
using Vakantiepark.Application.Services;
using Vakantiepark.Domain.Entities;
using Vakantiepark.Domain.Enums;
using Vakantiepark.Domain.Interfaces.Repositories;
using Vakantiepark.Shared.DTOs.Accommodation;
using Vakantiepark.Shared.DTOs.Price;
using Xunit;

namespace Vakantiepark.UnitTests
{
    // US-06 acceptance: availability per physical unit, capacity and pet limits,
    // preferred zones, high-season minimum nights as a search message.
    public class AccommodationSearchServiceTests
    {
        private readonly Mock<IAccommodationUnitRepository> _unitRepository = new();
        private readonly Mock<IAccommodationTypeRepository> _typeRepository = new();
        private readonly Mock<IRepository<AccommodationType>> _typeListRepository = new();
        private readonly Mock<IBookingRepository> _bookingRepository = new();
        private readonly Mock<IRepository<Season>> _seasonRepository = new();
        private readonly Mock<IRepository<ParkSettings>> _parkSettingsRepository = new();
        private readonly Mock<IRepository<Zone>> _zoneRepository = new();
        private readonly Mock<IPriceService> _priceService = new();
        private readonly AccommodationSearchService _sut;

        private readonly Zone _waterZone = new() { Id = Guid.NewGuid(), Name = "Aan het water", Category = AccommodationCategory.HolidayHome };
        private readonly Zone _forestZone = new() { Id = Guid.NewGuid(), Name = "Bosrand", Category = AccommodationCategory.HolidayHome };
        private readonly AccommodationType _berk = new() { Id = Guid.NewGuid(), Slug = "berk-4", Name = "Berk 4 personen", Category = AccommodationCategory.HolidayHome };
        private readonly AccommodationType _wilg = new() { Id = Guid.NewGuid(), Slug = "wilg-4", Name = "Wilg 4 personen", Category = AccommodationCategory.HolidayHome };
        private readonly Accommodation _berkA = CreateUnit("BERK-A", 4, 2);
        private readonly Accommodation _berkB = CreateUnit("BERK-B", 4, 0);
        private readonly Accommodation _wilgA = CreateUnit("WILG-A", 2, 0);

        // Search validation rejects past arrivals, so all fixtures use future dates.
        private static readonly DateOnly Arrival = new DateOnly(2027, 7, 10);
        private static readonly DateOnly Departure = new DateOnly(2027, 7, 17);

        public AccommodationSearchServiceTests()
        {
            // Type ids are minted per builder call, so units are built after the type
            // fixtures and pointed at their ids.
            _berkA.TypeId = _berk.Id;
            _berkA.ZoneId = _waterZone.Id;
            _berkA.Zone = _waterZone;
            _berkB.TypeId = _berk.Id;
            _berkB.ZoneId = _waterZone.Id;
            _berkB.Zone = _waterZone;
            _wilgA.TypeId = _wilg.Id;
            _wilgA.ZoneId = _forestZone.Id;
            _wilgA.Zone = _forestZone;

            _unitRepository.Setup(x => x.GetAllWithDetailsAsync()).ReturnsAsync([_berkA, _berkB, _wilgA]);
            _typeListRepository.Setup(x => x.GetAllAsync()).ReturnsAsync([_berk, _wilg]);
            _typeRepository.Setup(x => x.GetBySlugAsync("berk-4")).ReturnsAsync(_berk);
            _typeRepository.Setup(x => x.GetBySlugAsync("wilg-4")).ReturnsAsync(_wilg);
            _typeRepository.Setup(x => x.GetBySlugAsync("bestaatniet")).ReturnsAsync((AccommodationType?)null);
            _seasonRepository.Setup(x => x.GetAllAsync()).ReturnsAsync([new Season
            {
                Id = Guid.NewGuid(),
                Name = "Zomervakantie 2027",
                StartDate = new DateOnly(2027, 7, 5),
                EndDate = new DateOnly(2027, 8, 29),
                IsHighSeason = true,
            }]);
            _parkSettingsRepository.Setup(x => x.GetAllAsync()).ReturnsAsync([new ParkSettings
            {
                Id = Guid.NewGuid(),
                ArrivalTime = new TimeOnly(15, 0),
                DepartureTime = new TimeOnly(10, 0),
                HighSeasonMinimumNights = 7,
                DepositPercentage = 20m,
            }]);
            _zoneRepository.Setup(x => x.GetByIdAsync(_waterZone.Id)).ReturnsAsync(_waterZone);
            _zoneRepository.Setup(x => x.GetByIdAsync(_forestZone.Id)).ReturnsAsync(_forestZone);
            _priceService.Setup(x => x.CalculateAsync("berk-4", It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<int>()))
                .ReturnsAsync(new PriceSpecificationResponse { TotalPrice = 936m });
            _priceService.Setup(x => x.CalculateAsync("wilg-4", It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<int>()))
                .ReturnsAsync(new PriceSpecificationResponse { TotalPrice = 300m });

            _sut = new AccommodationSearchService(
                _unitRepository.Object,
                _typeRepository.Object,
                _typeListRepository.Object,
                _bookingRepository.Object,
                _seasonRepository.Object,
                _parkSettingsRepository.Object,
                _zoneRepository.Object,
                _priceService.Object);
        }

        [Fact]
        public async Task SearchAsync_UnitBookedInPeriod_TypesShowRemainingFreeUnits()
        {
            _bookingRepository
                .Setup(x => x.GetUnitIdsWithOverlappingBookingAsync(It.IsAny<List<Guid>>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
                .ReturnsAsync([_berkB.Id]);

            AccommodationSearchResponse result = await _sut.SearchAsync(null, Arrival, Departure, 4, 0, null);

            AccommodationTypeAvailability berk = Assert.Single(result.Results);
            Assert.Equal("berk-4", berk.Slug);
            Assert.Equal(1, berk.AvailableUnitCount);
            Assert.Equal(936m, berk.TotalPrice);
            Assert.Null(result.Message);
        }

        [Fact]
        public async Task SearchAsync_TypeSlugGiven_OnlyThatTypeIsSearched()
        {
            _bookingRepository
                .Setup(x => x.GetUnitIdsWithOverlappingBookingAsync(It.IsAny<List<Guid>>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
                .ReturnsAsync([]);

            AccommodationSearchResponse result = await _sut.SearchAsync("berk-4", Arrival, Departure, 2, 0, null);

            AccommodationTypeAvailability berk = Assert.Single(result.Results);
            Assert.Equal(2, berk.AvailableUnitCount);
        }

        [Fact]
        public async Task SearchAsync_PartyLargerThanEveryUnit_NoResultsWithOtherDatesSuggestion()
        {
            _bookingRepository
                .Setup(x => x.GetUnitIdsWithOverlappingBookingAsync(It.IsAny<List<Guid>>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
                .ReturnsAsync([]);

            AccommodationSearchResponse result = await _sut.SearchAsync(null, Arrival, Departure, 6, 0, null);

            Assert.Empty(result.Results);
            Assert.Equal("Geen vrije verblijven voor deze periode. Probeer andere data.", result.Message);
        }

        [Fact]
        public async Task SearchAsync_PetsRequested_UnitsWithoutPetAllowanceAreExcluded()
        {
            _bookingRepository
                .Setup(x => x.GetUnitIdsWithOverlappingBookingAsync(It.IsAny<List<Guid>>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
                .ReturnsAsync([]);

            AccommodationSearchResponse result = await _sut.SearchAsync("berk-4", Arrival, Departure, 4, 1, null);

            AccommodationTypeAvailability berk = Assert.Single(result.Results);
            Assert.Equal(1, berk.AvailableUnitCount);
        }

        [Fact]
        public async Task SearchAsync_PetsRequestedMoreThanUnitMaximum_TypeNotShown()
        {
            _bookingRepository
                .Setup(x => x.GetUnitIdsWithOverlappingBookingAsync(It.IsAny<List<Guid>>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
                .ReturnsAsync([]);

            AccommodationSearchResponse result = await _sut.SearchAsync("berk-4", Arrival, Departure, 4, 3, null);

            Assert.Empty(result.Results);
            Assert.NotNull(result.Message);
        }

        [Fact]
        public async Task SearchAsync_PrefZoneGiven_OnlyUnitsInThatZoneCount()
        {
            _bookingRepository
                .Setup(x => x.GetUnitIdsWithOverlappingBookingAsync(It.IsAny<List<Guid>>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
                .ReturnsAsync([]);

            AccommodationSearchResponse result = await _sut.SearchAsync(null, Arrival, Departure, 2, 0, _forestZone.Id);

            AccommodationTypeAvailability wilg = Assert.Single(result.Results);
            Assert.Equal("wilg-4", wilg.Slug);
            ZoneAvailability zone = Assert.Single(wilg.Zones);
            Assert.Equal("Bosrand", zone.Name);
            Assert.Equal(1, zone.AvailableUnitCount);
        }

        [Fact]
        public async Task SearchAsync_PrefZoneFullyBooked_MessageSuggestsOtherZone()
        {
            _bookingRepository
                .Setup(x => x.GetUnitIdsWithOverlappingBookingAsync(It.IsAny<List<Guid>>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
                .ReturnsAsync([_wilgA.Id]);

            AccommodationSearchResponse result = await _sut.SearchAsync(null, Arrival, Departure, 2, 0, _forestZone.Id);

            Assert.Empty(result.Results);
            Assert.Equal("In de gekozen zone is geen vrij verblijf voor deze periode. Kies een andere zone of probeer andere data.", result.Message);
        }

        [Fact]
        public async Task SearchAsync_UnknownPrefZone_ThrowsArgumentException()
        {
            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _sut.SearchAsync(null, Arrival, Departure, 2, 0, Guid.NewGuid()));

            Assert.Contains("De gekozen zone is niet gevonden.", exception.Message);
        }

        [Fact]
        public async Task SearchAsync_OverlapsHighSeasonShorterThanMinimum_MessageNamesTheMinimum()
        {
            AccommodationSearchResponse result = await _sut.SearchAsync(null, new DateOnly(2027, 7, 10), new DateOnly(2027, 7, 15), 2, 0, null);

            Assert.Empty(result.Results);
            Assert.Contains("In het hoogseizoen (Zomervakantie 2027) moet een boeking minimaal 7 nachten zijn.", result.Message);
            _priceService.Verify(x => x.CalculateAsync(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task SearchAsync_StayEndingJustBeforeHighSeason_HasNoMinimumNightsRule()
        {
            _bookingRepository
                .Setup(x => x.GetUnitIdsWithOverlappingBookingAsync(It.IsAny<List<Guid>>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
                .ReturnsAsync([]);

            AccommodationSearchResponse result = await _sut.SearchAsync(null, new DateOnly(2027, 6, 28), new DateOnly(2027, 7, 4), 2, 0, null);

            Assert.NotEmpty(result.Results);
            Assert.Null(result.Message);
        }

        [Fact]
        public async Task SearchAsync_ResultsAreOrderedByTotalPrice()
        {
            _bookingRepository
                .Setup(x => x.GetUnitIdsWithOverlappingBookingAsync(It.IsAny<List<Guid>>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
                .ReturnsAsync([]);

            AccommodationSearchResponse result = await _sut.SearchAsync(null, Arrival, Departure, 2, 0, null);

            Assert.Equal(2, result.Results.Count);
            Assert.Equal("wilg-4", result.Results[0].Slug);
            Assert.Equal("berk-4", result.Results[1].Slug);
        }

        [Fact]
        public async Task SearchAsync_UnknownTypeSlug_NoResults()
        {
            AccommodationSearchResponse result = await _sut.SearchAsync("bestaatniet", Arrival, Departure, 2, 0, null);

            Assert.Empty(result.Results);
        }

        [Fact]
        public async Task SearchAsync_ArrivalInPast_ThrowsArgumentException()
        {
            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _sut.SearchAsync(null, new DateOnly(2020, 1, 10), new DateOnly(2020, 1, 15), 2, 0, null));

            Assert.Contains("De aankomstdatum ligt in het verleden.", exception.Message);
        }

        [Fact]
        public async Task SearchAsync_DepartureNotAfterArrival_ThrowsArgumentException()
        {
            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _sut.SearchAsync(null, Arrival, Arrival, 2, 0, null));

            Assert.Contains("De vertrekdatum moet na de aankomstdatum liggen.", exception.Message);
        }

        [Fact]
        public async Task SearchAsync_NegativePets_ThrowsArgumentException()
        {
            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _sut.SearchAsync(null, Arrival, Departure, 2, -1, null));

            Assert.Contains("Het aantal huisdieren kan niet negatief zijn.", exception.Message);
        }

        private static Accommodation CreateUnit(string code, int maxPersons, int maxPets) => new()
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = code,
            MaxPersons = maxPersons,
            Features =
            [
                new AccommodationFeature
                {
                    NumberValue = maxPets,
                    Feature = new Feature { Key = "maxPets", Name = "Maximaal aantal huisdieren" },
                },
            ],
        };
    }
}
