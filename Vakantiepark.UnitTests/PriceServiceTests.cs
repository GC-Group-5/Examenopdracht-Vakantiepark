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
        private const decimal HuisdierSchoonmaak = 25m;

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
        public async Task CalculateAsync_WithPets_AddsConfiguredPetCleaningFeeOnce()
        {
            PriceSpecificationResponse price = await _sut.CalculateAsync(
                "berk-4", new DateOnly(2026, 7, 10), new DateOnly(2026, 7, 17), 4, 2);

            Assert.Equal(HuisdierSchoonmaak, price.PetCleaningFee);
            Assert.Equal(936m + HuisdierSchoonmaak, price.TotalPrice);
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
            PetCleaningFee = HuisdierSchoonmaak,
        };
    }
}
