using Moq;
using Vakantiepark.Application.Helpers;
using Vakantiepark.Application.Services;
using Vakantiepark.Domain.Entities;
using Vakantiepark.Domain.Interfaces.Repositories;
using Vakantiepark.Infrastructure.Persistence.Seeders;
using Vakantiepark.Shared.DTOs.Accommodation;
using Xunit;

namespace Vakantiepark.UnitTests
{
    // US-04: the overview and the detail page per type, built from the real export, so the
    // chips can be compared with the website template (accommodaties/*.html).
    public class AccommodationTypeTests
    {
        private static readonly DateTime Now = new(2026, 11, 13, 9, 0, 0, DateTimeKind.Utc);

        private readonly Mock<IAccommodationUnitRepository> _accommodationRepository = new Mock<IAccommodationUnitRepository>();
        private readonly Mock<IFeatureRepository> _featureRepository = new Mock<IFeatureRepository>();

        public AccommodationTypeTests()
        {
            AccommodationImport import = AccommodationSeeder.BuildImport(AccommodationSeeder.LoadExport(), Now);
            ParkConfigurationSeeder.ApplyTypeRates(import.Types);
            _accommodationRepository.Setup(x => x.GetAllWithDetailsAsync()).ReturnsAsync(import.Accommodations);
        }

        private AccommodationService CreateService() => new AccommodationService(_accommodationRepository.Object, _featureRepository.Object);

        public static TheoryData<string, string[]> TemplateChips => new()
        {
            { "berk-4", ["2 slaapkamers", "1 badkamer", "Toegankelijk", "Vaatwasser", "Wasmachine", "Airco", "Haard"] },
            { "berk-6", ["3 slaapkamers", "1 badkamer", "Vaatwasser", "Wasmachine", "Airco", "Haard", "Overdekt terras"] },
            { "eik-6", ["3 slaapkamers", "1–2 badkamers", "Sauna mogelijk", "Vaatwasser", "Wasmachine", "Airco", "Haard"] },
            { "eik-luxe-6", ["3 slaapkamers", "2 badkamers", "Toegankelijk", "Sauna mogelijk", "Hottub mogelijk", "Vaatwasser", "Wasmachine"] },
            { "beuk-8", ["4 slaapkamers", "2–3 badkamers", "Toegankelijk", "Sauna mogelijk", "Vaatwasser", "Wasmachine", "Airco"] },
            { "wilg-4-mindervalide", ["2 slaapkamers", "1 badkamer", "Toegankelijk", "Vaatwasser", "Wasmachine", "Airco", "Overdekt terras"] },
            { "bosvilla-10", ["5 slaapkamers", "2–3 badkamers", "Sauna mogelijk", "Hottub mogelijk", "Vaatwasser", "Wasmachine", "Airco"] },
            { "tentplaats-basis", ["Stroom 6/10A", "Huisdieren mogelijk"] },
            { "tentplaats-comfort", ["Stroom 10/16A", "Wateraansluiting mogelijk", "Privé sanitair mogelijk", "Huisdieren mogelijk"] },
            { "caravanplaats", ["Stroom 10/16A", "Wateraansluiting mogelijk", "Afvoer mogelijk", "Privé sanitair mogelijk", "Caravan tot 8 m", "Huisdieren mogelijk"] },
            { "camperplaats", ["Stroom 10/16A", "Wateraansluiting mogelijk", "Afvoer mogelijk", "Camper tot 8.5 m", "Huisdieren mogelijk"] },
            { "camperplaats-xl", ["Stroom 16A", "Wateraansluiting mogelijk", "Afvoer mogelijk", "Privé sanitair mogelijk", "Camper tot 11 m", "Huisdieren mogelijk"] },
        };

        [Theory]
        [MemberData(nameof(TemplateChips))]
        public async Task GetTypeBySlugAsync_Highlights_MatchTheTemplate(string slug, string[] expected)
        {
            AccommodationTypeResponse type = await CreateService().GetTypeBySlugAsync(slug);

            Assert.Equal(expected.ToList(), type.Highlights);
        }

        [Fact]
        public async Task GetTypeBySlugAsync_BerkFour_CombinesAllUnitsOfTheType()
        {
            AccommodationTypeResponse type = await CreateService().GetTypeBySlugAsync("berk-4");

            Assert.Equal("Berk 4", type.Name);
            Assert.Equal("HolidayHome", type.Category);
            Assert.Equal(32, type.UnitCount);
            Assert.Equal(4, type.MaxPersons);
            Assert.Equal("Compact en sfeervol voor een ontspannen verblijf met z’n tweeën of met het gezin.", type.Description);
            Assert.Equal(new List<string> { "Bosrand", "Heideveld", "Parkcentrum", "Rustweide", "Waterkant" }, type.Zones);
        }

        [Fact]
        public async Task GetTypeBySlugAsync_UnknownSlug_ThrowsKeyNotFoundException()
        {
            await Assert.ThrowsAsync<KeyNotFoundException>(() => CreateService().GetTypeBySlugAsync("boomhut-2"));
        }

        [Fact]
        public async Task GetTypesAsync_AllTwelveTypes_HolidayHomesFirst()
        {
            List<AccommodationTypeResponse> types = await CreateService().GetTypesAsync();

            Assert.Equal(12, types.Count);
            Assert.Equal(150, types.Where(type => type.Category == "HolidayHome").Sum(type => type.UnitCount));
            Assert.Equal(50, types.Where(type => type.Category == "CampingPitch").Sum(type => type.UnitCount));
            Assert.All(types.Take(7), type => Assert.Equal("HolidayHome", type.Category));
            Assert.Equal("berk-4", types[0].Slug);
            Assert.Equal("camperplaats-xl", types[^1].Slug);
        }

        [Fact]
        public void For_NoUnits_ReturnsNoHighlights()
        {
            Assert.Empty(AccommodationTypeHighlights.For(new List<Accommodation>()));
        }
    }
}
