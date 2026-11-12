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

        private AccommodationService CreateService() => new AccommodationService(_accommodationRepository.Object, _featureRepository.Object);

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
    }
}
