using Moq;
using Vakantiepark.Application.Services;
using Vakantiepark.Domain.Entities;
using Vakantiepark.Domain.Enums;
using Vakantiepark.Domain.Interfaces.Repositories;
using Vakantiepark.Shared.DTOs.Accommodation;
using Xunit;

namespace Vakantiepark.UnitTests
{
    public class AccommodationServiceTests
    {
        private readonly Mock<IAccommodationUnitRepository> _accommodationRepository = new Mock<IAccommodationUnitRepository>();
        private readonly Mock<IFeatureRepository> _featureRepository = new Mock<IFeatureRepository>();
        private readonly Mock<IRepository<Zone>> _zoneRepository = new Mock<IRepository<Zone>>();
        private readonly Mock<IRepository<Season>> _seasonRepository = new Mock<IRepository<Season>>();
        private readonly Mock<IRepository<AccommodationTypePrice>> _priceRepository = new Mock<IRepository<AccommodationTypePrice>>();

        // The coming 12 months then run from 1 January 2027 up to 1 January 2028: 365 nights.
        private static readonly DateOnly Today = new DateOnly(2027, 1, 1);

        private AccommodationService CreateService() => new AccommodationService(
            _accommodationRepository.Object,
            _featureRepository.Object,
            _zoneRepository.Object,
            _seasonRepository.Object,
            _priceRepository.Object,
            new FixedTimeProvider(new DateTimeOffset(Today.ToDateTime(new TimeOnly(9, 0)), TimeSpan.Zero)));

        // One Berk 4 with a default price of 100 euro per night and no seasons yet.
        private AccommodationType SetUpBerkFour()
        {
            AccommodationType type = TestData.CreateAccommodationType(berkFour => berkFour.DefaultPricePerNight = 100m);
            _accommodationRepository
                .Setup(x => x.GetAllWithDetailsAsync())
                .ReturnsAsync([TestData.CreateAccommodation("B4-001", configure: unit => unit.Type = type)]);
            _seasonRepository.Setup(x => x.GetAllAsync()).ReturnsAsync([]);
            _priceRepository.Setup(x => x.GetAllAsync()).ReturnsAsync([]);
            return type;
        }

        private static Accommodation CreatePitch()
        {
            Feature surface = new() { Key = "surface", Name = "Ondergrond", ValueType = FeatureValueType.Option };
            FeatureOption paved = new() { Value = "paved", Name = "Verhard", Feature = surface };
            surface.Options = [paved];

            return new Accommodation
            {
                Code = "CP-001",
                Name = "Camperplaats 01",
                MaxPersons = 4,
                Type = TestData.CreateAccommodationType(type =>
                {
                    type.Name = "Camperplaats";
                    type.Slug = "camperplaats";
                    type.Category = AccommodationCategory.CampingPitch;
                }),
                Zone = new Zone { Name = "Waterweide", Category = AccommodationCategory.CampingPitch },
                Features =
                [
                    new AccommodationFeature { Feature = new Feature { Key = "maxPets", Name = "Maximaal aantal huisdieren", ValueType = FeatureValueType.Number }, NumberValue = 2m },
                    new AccommodationFeature { Feature = surface, Option = paved },
                    new AccommodationFeature { Feature = new Feature { Key = "carAtPitch", Name = "Auto bij de plaats", ValueType = FeatureValueType.Boolean } },
                ],
            };
        }

        [Fact]
        public async Task GetByCodeAsync_UnknownCode_ThrowsKeyNotFoundException()
        {
            _accommodationRepository.Setup(x => x.GetByCodeWithDetailsAsync("XX-999")).ReturnsAsync((Accommodation?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => CreateService().GetByCodeAsync("XX-999"));
        }

        [Fact]
        public async Task GetByCodeAsync_LowercaseCode_FindsTheUnit()
        {
            _accommodationRepository.Setup(x => x.GetByCodeWithDetailsAsync("CP-001")).ReturnsAsync(CreatePitch());

            AccommodationResponse response = await CreateService().GetByCodeAsync(" cp-001 ");

            Assert.Equal("CP-001", response.Code);
        }

        [Fact]
        public async Task GetByCodeAsync_MapsEveryKindOfFeatureValue()
        {
            _accommodationRepository.Setup(x => x.GetByCodeWithDetailsAsync("CP-001")).ReturnsAsync(CreatePitch());

            AccommodationResponse response = await CreateService().GetByCodeAsync("CP-001");

            Assert.Equal("Waterweide", response.Zone);
            Assert.Equal("CampingPitch", response.Category);
            Assert.Equal(2m, response.Features.Single(x => x.Key == "maxPets").NumberValue);
            AccommodationFeatureResponse surface = response.Features.Single(x => x.Key == "surface");
            Assert.Equal("Option", surface.ValueType);
            Assert.Equal("paved", surface.OptionValue);
            Assert.Equal("Verhard", surface.OptionName);
            AccommodationFeatureResponse car = response.Features.Single(x => x.Key == "carAtPitch");
            Assert.Null(car.NumberValue);
            Assert.Null(car.OptionValue);
        }

        [Fact]
        public async Task GetFeaturesAsync_ReturnsOptionsInSortOrder()
        {
            Feature shade = new() { Key = "shade", Name = "Schaduw", ValueType = FeatureValueType.Option };
            shade.Options =
            [
                new FeatureOption { Value = "high", Name = "Veel", SortOrder = 1 },
                new FeatureOption { Value = "none", Name = "Geen", SortOrder = 0 },
            ];
            _featureRepository.Setup(x => x.GetAllWithOptionsAsync()).ReturnsAsync(new List<Feature> { shade });

            List<FeatureResponse> features = await CreateService().GetFeaturesAsync();

            Assert.Equal(new[] { "none", "high" }, features.Single().Options.Select(option => option.Value));
        }

        [Fact]
        public async Task GetTypeBySlugAsync_NoSeasons_AveragePriceIsTheDefaultPrice()
        {
            SetUpBerkFour();

            AccommodationTypeResponse type = await CreateService().GetTypeBySlugAsync("berk-4");

            Assert.Equal(100m, type.AveragePricePerNight);
        }

        [Fact]
        public async Task GetTypeBySlugAsync_SummerSeason_AveragesTheNightsOfTheComingYear()
        {
            AccommodationType berkFour = SetUpBerkFour();
            Season summer = TestData.CreateSeason(season => { season.StartDate = new DateOnly(2027, 7, 1); season.EndDate = new DateOnly(2027, 7, 31); });
            _seasonRepository.Setup(x => x.GetAllAsync()).ReturnsAsync([summer]);
            _priceRepository.Setup(x => x.GetAllAsync()).ReturnsAsync([TestData.CreateAccommodationTypePrice(berkFour, summer, 200m)]);

            AccommodationTypeResponse type = await CreateService().GetTypeBySlugAsync("berk-4");

            // 334 nights × 100 + 31 summer nights × 200 = 39600 euro over 365 nights.
            Assert.Equal(108.49m, type.AveragePricePerNight);
        }

        [Fact]
        public async Task GetTypeBySlugAsync_SeasonPriceOfAnotherType_IsIgnored()
        {
            SetUpBerkFour();
            AccommodationType otherType = TestData.CreateAccommodationType(beukEight => beukEight.Slug = "beuk-8");
            Season summer = TestData.CreateSeason(season => { season.StartDate = new DateOnly(2027, 7, 1); season.EndDate = new DateOnly(2027, 7, 31); });
            _seasonRepository.Setup(x => x.GetAllAsync()).ReturnsAsync([summer]);
            _priceRepository.Setup(x => x.GetAllAsync()).ReturnsAsync([TestData.CreateAccommodationTypePrice(otherType, summer, 500m)]);

            AccommodationTypeResponse type = await CreateService().GetTypeBySlugAsync("berk-4");

            Assert.Equal(100m, type.AveragePricePerNight);
        }

        [Fact]
        public async Task GetTypesAsync_EveryType_GetsAnAveragePrice()
        {
            SetUpBerkFour();

            List<AccommodationTypeResponse> types = await CreateService().GetTypesAsync();

            Assert.Equal(100m, types.Single().AveragePricePerNight);
        }
    }
}
