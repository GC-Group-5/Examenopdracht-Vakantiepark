using Moq;
using Vakantiepark.Application.Services;
using Vakantiepark.Domain.Entities;
using Vakantiepark.Domain.Interfaces.Repositories;
using Vakantiepark.Shared.DTOs.Price;
using Xunit;

namespace Vakantiepark.UnitTests
{
    public class PriceServiceTests
    {
        // Fixture night rates for berk-4, mirroring the merged seeder calendar:
        // Voorjaarsvakantie 86, Pinksteren 105, Zomervakantie 113 (high season),
        // Herfstvakantie 86, Kerstvakantie 83; default night price 90; cleaning 75.
        private const decimal VoorjaarPrijs = 86m;
        private const decimal PinksterenPrijs = 105m;
        private const decimal ZomerPrijs = 113m;
        private const decimal HerfstPrijs = 86m;
        private const decimal KerstPrijs = 83m;
        private const decimal Schoonmaak = 75m;
        private const decimal Belasting = 2.50m;
        private const decimal NieuweBelasting = 3.00m;
        private const decimal StandaardPrijs = 90m;
        private const decimal AanbetalingsPercentage = 20m;

        private readonly Mock<IAccommodationTypeRepository> _typeRepository = new();
        private readonly Mock<IRepository<Season>> _seasonRepository = new();
        private readonly Mock<IRepository<AccommodationTypePrice>> _priceRepository = new();
        private readonly Mock<IRepository<TouristTaxRate>> _taxRateRepository = new();
        private readonly Mock<IRepository<ParkSettings>> _parkSettingsRepository = new();
        private readonly Guid _typeId;
        private readonly PriceService _sut;

        public PriceServiceTests()
        {
            AccommodationType type = TestData.CreateAccommodationType(t =>
            {
                t.Slug = "berk-4";
                t.CleaningFee = Schoonmaak;
                t.DefaultPricePerNight = StandaardPrijs;
            });
            _typeId = type.Id;

            Season voorjaar = TestData.CreateSeason(s => { s.Name = "Voorjaarsvakantie 2026"; s.StartDate = new DateOnly(2026, 2, 14); s.EndDate = new DateOnly(2026, 3, 1); });
            Season pinksteren = TestData.CreateSeason(s => { s.Name = "Pinksteren 2026"; s.StartDate = new DateOnly(2026, 5, 22); s.EndDate = new DateOnly(2026, 5, 26); });
            Season zomer = TestData.CreateSeason(s => { s.Name = "Zomervakantie 2026"; s.StartDate = new DateOnly(2026, 7, 4); s.EndDate = new DateOnly(2026, 8, 30); s.IsHighSeason = true; });
            Season herfst = TestData.CreateSeason(s => { s.Name = "Herfstvakantie 2026"; s.StartDate = new DateOnly(2026, 10, 10); s.EndDate = new DateOnly(2026, 10, 25); });
            Season kerst = TestData.CreateSeason(s => { s.Name = "Kerstvakantie 2026"; s.StartDate = new DateOnly(2026, 12, 19); s.EndDate = new DateOnly(2027, 1, 3); });

            List<AccommodationTypePrice> prices = [
                TypePrice(_typeId, voorjaar.Id, VoorjaarPrijs),
                TypePrice(_typeId, pinksteren.Id, PinksterenPrijs),
                TypePrice(_typeId, zomer.Id, ZomerPrijs),
                TypePrice(_typeId, herfst.Id, HerfstPrijs),
                TypePrice(_typeId, kerst.Id, KerstPrijs),
            ];

            _typeRepository.Setup(x => x.GetBySlugAsync("berk-4")).ReturnsAsync(type);
            _seasonRepository.Setup(x => x.GetAllAsync()).ReturnsAsync([voorjaar, pinksteren, zomer, herfst, kerst]);
            _priceRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(prices);
            _taxRateRepository.Setup(x => x.GetAllAsync()).ReturnsAsync([TaxRate(Belasting, new DateOnly(2026, 1, 1))]);
            _parkSettingsRepository.Setup(x => x.GetAllAsync()).ReturnsAsync([ParkSettings()]);

            _sut = new PriceService(
                _typeRepository.Object,
                _seasonRepository.Object,
                _priceRepository.Object,
                _taxRateRepository.Object,
                _parkSettingsRepository.Object);
        }

        [Fact]
        public async Task CalculateAsync_WithinOneSeason_UsesThatSeasonsRatePerNight()
        {
            PriceSpecificationResponse price = await _sut.CalculateAsync("berk-4", new DateOnly(2026, 7, 10), new DateOnly(2026, 7, 17), 4);

            Assert.Equal(7 * ZomerPrijs, price.AccommodationPrice);
            // 791 verblijf + 75 schoonmaak + 4 gasten × 7 nachten × 2.50 = 70 belasting.
            Assert.Equal(936m, price.TotalPrice);
        }

        [Fact]
        public async Task CalculateAsync_DepositIsPercentageOfTotal()
        {
            PriceSpecificationResponse price = await _sut.CalculateAsync("berk-4", new DateOnly(2026, 7, 10), new DateOnly(2026, 7, 17), 4);

            Assert.Equal(Math.Round(936m * AanbetalingsPercentage / 100m, 2), price.Deposit);
        }

        [Fact]
        public async Task CalculateAsync_AcrossSeasonBoundary_MixesSeasonRateAndDefaultPrice()
        {
            // 2027-05-20 and -21 fall in no season (default night price),
            // 2027-05-22 and -23 are the first nights of Pinksteren 2027.
            Season pinksteren2027 = TestData.CreateSeason(s => { s.Name = "Pinksteren 2027"; s.StartDate = new DateOnly(2027, 5, 22); s.EndDate = new DateOnly(2027, 5, 26); });
            _seasonRepository.Setup(x => x.GetAllAsync()).ReturnsAsync([pinksteren2027]);
            _priceRepository.Setup(x => x.GetAllAsync()).ReturnsAsync([TypePrice(_typeId, pinksteren2027.Id, PinksterenPrijs)]);

            PriceSpecificationResponse price = await _sut.CalculateAsync("berk-4", new DateOnly(2027, 5, 20), new DateOnly(2027, 5, 24), 4);

            Assert.Equal(2 * StandaardPrijs + 2 * PinksterenPrijs, price.AccommodationPrice);
        }

        [Fact]
        public async Task CalculateAsync_NightOutsideEverySeason_UsesDefaultNightPrice()
        {
            // School-holiday seasons leave most of the year uncovered; those
            // nights must fall back to the type's default night price.
            PriceSpecificationResponse price = await _sut.CalculateAsync("berk-4", new DateOnly(2027, 4, 10), new DateOnly(2027, 4, 15), 2);

            Assert.Equal(5 * StandaardPrijs, price.AccommodationPrice);
        }

        [Fact]
        public async Task CalculateAsync_NoSeasonsConfigured_UsesDefaultNightPrice()
        {
            _seasonRepository.Setup(x => x.GetAllAsync()).ReturnsAsync([]);

            PriceSpecificationResponse price = await _sut.CalculateAsync("berk-4", new DateOnly(2026, 7, 10), new DateOnly(2026, 7, 13), 2);

            Assert.Equal(3 * StandaardPrijs, price.AccommodationPrice);
        }

        [Fact]
        public async Task CalculateAsync_SeasonWithoutRateForType_UsesDefaultNightPrice()
        {
            _priceRepository.Setup(x => x.GetAllAsync()).ReturnsAsync([]);

            PriceSpecificationResponse price = await _sut.CalculateAsync("berk-4", new DateOnly(2026, 7, 10), new DateOnly(2026, 7, 12), 2);

            Assert.Equal(2 * StandaardPrijs, price.AccommodationPrice);
        }

        [Fact]
        public async Task CalculateAsync_AddsCleaningExactlyOnce_AndTaxPerPersonPerNight()
        {
            PriceSpecificationResponse price = await _sut.CalculateAsync("berk-4", new DateOnly(2026, 7, 10), new DateOnly(2026, 7, 17), 4);

            Assert.Equal(Schoonmaak, price.CleaningFee);
            Assert.Equal(4 * 7 * Belasting, price.TouristTax);
        }

        [Fact]
        public async Task CalculateAsync_NewTaxRateMidStay_PricesEachNightWithTheRateValidThatNight()
        {
            // De belasting stijgt per 1 juli 2026 van 2.50 naar 3.00; nachten van
            // vóór 1 juli houden het oude tarief.
            _taxRateRepository.Setup(x => x.GetAllAsync()).ReturnsAsync([
                TaxRate(Belasting, new DateOnly(2026, 1, 1)),
                TaxRate(NieuweBelasting, new DateOnly(2026, 7, 1)),
            ]);

            PriceSpecificationResponse price = await _sut.CalculateAsync("berk-4", new DateOnly(2026, 6, 29), new DateOnly(2026, 7, 6), 4);

            Assert.Equal(2 * 4 * Belasting + 5 * 4 * NieuweBelasting, price.TouristTax);
        }

        [Fact]
        public async Task CalculateAsync_DepartureNotAfterArrival_ThrowsDutchMessage()
        {
            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _sut.CalculateAsync("berk-4", new DateOnly(2026, 7, 10), new DateOnly(2026, 7, 10), 2));

            Assert.Contains("De vertrekdatum moet na de aankomstdatum liggen.", exception.Message);
        }

        [Fact]
        public async Task CalculateAsync_UnknownTypeSlug_ThrowsDutchMessage()
        {
            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _sut.CalculateAsync("bestaatniet", new DateOnly(2026, 7, 10), new DateOnly(2026, 7, 12), 2));

            Assert.Contains("Onbekende verblijfstype", exception.Message);
        }

        [Fact]
        public async Task CalculateAsync_NoTaxRateConfigured_ThrowsDutchMessage()
        {
            _taxRateRepository.Setup(x => x.GetAllAsync()).ReturnsAsync([]);

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _sut.CalculateAsync("berk-4", new DateOnly(2026, 7, 10), new DateOnly(2026, 7, 12), 2));

            Assert.Equal("Er is nog geen toeristenbelastingtarief ingesteld.", exception.Message);
        }

        [Fact]
        public async Task CalculateAsync_NoParkSettingsConfigured_ThrowsDutchMessage()
        {
            _parkSettingsRepository.Setup(x => x.GetAllAsync()).ReturnsAsync([]);

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _sut.CalculateAsync("berk-4", new DateOnly(2026, 7, 10), new DateOnly(2026, 7, 12), 2));

            Assert.Equal("De parkinstellingen zijn niet ingesteld.", exception.Message);
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

        private static ParkSettings ParkSettings() => new()
        {
            Id = Guid.NewGuid(),
            ArrivalTime = new TimeOnly(15, 0),
            DepartureTime = new TimeOnly(10, 0),
            HighSeasonMinimumNights = 7,
            DepositPercentage = AanbetalingsPercentage,
        };
    }
}
