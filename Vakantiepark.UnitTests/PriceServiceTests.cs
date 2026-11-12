using Moq;
using Vakantiepark.Application.Services;
using Vakantiepark.Domain.Entities;
using Vakantiepark.Domain.Enums;
using Vakantiepark.Domain.Interfaces.Repositories;
using Vakantiepark.Shared.DTOs.Price;
using Xunit;

namespace Vakantiepark.UnitTests
{
    public class PriceServiceTests
    {
        // Berk 4 test rates from the US-05 seeder: laag 75, midden 95, hoog 135, najaar 85.
        private const decimal LaagPrijs = 75m;
        private const decimal MiddenPrijs = 95m;
        private const decimal HoogPrijs = 135m;
        private const decimal NajaarPrijs = 85m;
        private const decimal Schoonmaak = 75m;
        private const decimal Belasting = 2.50m;
        private const decimal NieuweBelasting = 3.00m;
        private const decimal StandaardPrijs = 90m;

        private readonly Mock<IRepository<AccommodationType>> _typeRepository = new();
        private readonly Mock<IRepository<Season>> _seasonRepository = new();
        private readonly Mock<IRepository<AccommodationTypePrice>> _priceRepository = new();
        private readonly Mock<IRepository<TouristTaxRate>> _taxRateRepository = new();
        private readonly PriceService _sut;

        public PriceServiceTests()
        {
            AccommodationType type = TestData.CreateAccommodationType(t =>
            {
                t.CleaningFee = Schoonmaak;
                t.DefaultPricePerNight = StandaardPrijs;
            });
            Guid typeId = type.Id;

            Season laag =TestData.CreateSeason(s => { s.Name = "Laagseizoen"; s.StartDate = new DateOnly(2026, 1, 1); s.EndDate = new DateOnly(2026, 3, 31); });
            Season midden = TestData.CreateSeason(s => { s.Name = "Middenseizoen"; s.StartDate = new DateOnly(2026, 4, 1); s.EndDate = new DateOnly(2026, 6, 30); });
            Season hoog = TestData.CreateSeason(s => { s.Name = "Hoogseizoen"; s.StartDate = new DateOnly(2026, 7, 1); s.EndDate = new DateOnly(2026, 8, 31); s.IsHighSeason = true; });
            Season najaar = TestData.CreateSeason(s => { s.Name = "Najaar"; s.StartDate = new DateOnly(2026, 9, 1); s.EndDate = new DateOnly(2026, 12, 31); });

            List<AccommodationTypePrice> prices = [
                TypePrice(typeId, laag.Id, LaagPrijs),
                TypePrice(typeId, midden.Id, MiddenPrijs),
                TypePrice(typeId, hoog.Id, HoogPrijs),
                TypePrice(typeId, najaar.Id, NajaarPrijs),
            ];

            _typeRepository.Setup(x => x.GetAllAsync()).ReturnsAsync([type]);
            _seasonRepository.Setup(x => x.GetAllAsync()).ReturnsAsync([laag, midden, hoog, najaar]);
            _priceRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(prices);
            _taxRateRepository.Setup(x => x.GetAllAsync()).ReturnsAsync([TaxRate(Belasting, new DateOnly(2026, 1, 1))]);

            _sut = new PriceService(
                _typeRepository.Object,
                _seasonRepository.Object,
                _priceRepository.Object,
                _taxRateRepository.Object);
        }

        [Fact]
        public async Task CalculateAsync_WithinOneSeason_UsesThatSeasonsRatePerNight()
        {
            PriceSpecificationResponse price = await _sut.CalculateAsync("berk-4", new DateOnly(2026, 4, 10), new DateOnly(2026, 4, 15), 2);

            Assert.Equal(5 * MiddenPrijs, price.AccommodationPrice);
            // 475 verblijf + 75 schoonmaak + 2 gasten × 5 nachten × 2.50 = 25 belasting.
            Assert.Equal(575m, price.TotalPrice);
        }

        [Fact]
        public async Task CalculateAsync_AcrossSeasonBoundary_MixesBothSeasonRates()
        {
            // 2026-06-29 and -30 are midden, 07-01 and -02 are hoog.
            PriceSpecificationResponse price = await _sut.CalculateAsync("berk-4", new DateOnly(2026, 6, 29), new DateOnly(2026, 7, 3), 4);

            Assert.Equal(2 * MiddenPrijs + 2 * HoogPrijs, price.AccommodationPrice);
        }

        [Fact]
        public async Task CalculateAsync_AddsCleaningExactlyOnce_AndTaxPerPersonPerNight()
        {
            PriceSpecificationResponse price = await _sut.CalculateAsync("berk-4", new DateOnly(2026, 4, 10), new DateOnly(2026, 4, 15), 2);

            Assert.Equal(Schoonmaak, price.CleaningFee);
            Assert.Equal(2 * 5 * Belasting, price.TouristTax);
        }

        [Fact]
        public async Task CalculateAsync_TotalIsVerblijfPlusSchoonmaakPlusBelasting()
        {
            PriceSpecificationResponse price = await _sut.CalculateAsync("berk-4", new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 6), 3);

            Assert.Equal(5 * NajaarPrijs + Schoonmaak + 3 * 5 * Belasting, price.TotalPrice);
        }

        [Fact]
        public async Task CalculateAsync_DepartureNotAfterArrival_ThrowsDutchMessage()
        {
            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _sut.CalculateAsync("berk-4", new DateOnly(2026, 4, 10), new DateOnly(2026, 4, 10), 2));

            Assert.Contains("De vertrekdatum moet na de aankomstdatum liggen.", exception.Message);
        }

        [Fact]
        public async Task CalculateAsync_NightOutsideAllSeasons_ThrowsDutchMessage()
        {
            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _sut.CalculateAsync("berk-4", new DateOnly(2025, 12, 30), new DateOnly(2026, 1, 2), 2));

            Assert.Equal("Voor de nacht van 30-12-2025 is geen seizoen ingesteld.", exception.Message);
        }

        [Fact]
        public async Task CalculateAsync_NieuwBelastingtariefHalfWeg_MerekentPerNachtMetGeldigTarief()
        {
            // De belasting stijgt per 1 juli 2026 van 2.50 naar 3.00; nachten van
            // vóór 1 juli houden het oude tarief.
            _taxRateRepository.Setup(x => x.GetAllAsync()).ReturnsAsync([
                TaxRate(Belasting, new DateOnly(2026, 1, 1)),
                TaxRate(NieuweBelasting, new DateOnly(2026, 7, 1)),
            ]);

            PriceSpecificationResponse price = await _sut.CalculateAsync("berk-4", new DateOnly(2026, 6, 29), new DateOnly(2026, 7, 3), 4);

            Assert.Equal(2 * 4 * Belasting + 2 * 4 * NieuweBelasting, price.TouristTax);
        }

        [Fact]
        public async Task CalculateAsync_ZeizoenZonderPrijsVoorType_ValtTerugOpStandaardNachtprijs()
        {
            _priceRepository.Setup(x => x.GetAllAsync()).ReturnsAsync([]);

            PriceSpecificationResponse price = await _sut.CalculateAsync("berk-4", new DateOnly(2026, 4, 10), new DateOnly(2026, 4, 12), 2);

            Assert.Equal(2 * StandaardPrijs, price.AccommodationPrice);
        }

        [Fact]
        public async Task CalculateAsync_UnknownTypeSlug_ThrowsDutchMessage()
        {
            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _sut.CalculateAsync("bestaatniet", new DateOnly(2026, 4, 10), new DateOnly(2026, 4, 12), 2));

            Assert.Contains("Onbekende verblijfstype", exception.Message);
        }

        private static AccommodationTypePrice TypePrice(Guid typeId, Guid seasonId, decimal price) => new()
        {
            Id = Guid.NewGuid(),
            PricePerNight = price,
            AccommodationTypeId = typeId,
            SeasonId = seasonId,
        };

        private static TouristTaxRate TaxRate(decimal amount, DateOnly validFrom) => new()
        {
            Id = Guid.NewGuid(),
            AmountPerPersonPerNight = amount,
            ValidFrom = validFrom,
        };
    }
}
