using Moq;
using Vakantiepark.Application.Extensions;
using Vakantiepark.Application.Services;
using Vakantiepark.Domain.Entities;
using Vakantiepark.Domain.Interfaces.Repositories;
using Vakantiepark.Shared.DTOs.Park;
using Xunit;

namespace Vakantiepark.UnitTests
{
    // US-18: the beheerder changes park rules, tourist tax, seasons and prices.
    public class ParkSettingsServiceTests
    {
        private readonly Mock<IRepository<ParkSettings>> _settingsRepository = new();
        private readonly Mock<IRepository<TouristTaxRate>> _taxRepository = new();
        private readonly Mock<IRepository<Season>> _seasonRepository = new();
        private readonly Mock<IRepository<AccommodationTypePrice>> _priceRepository = new();
        private readonly Mock<IAccommodationTypeRepository> _typeRepository = new();
        private readonly ParkSettingsService _sut;

        private readonly ParkSettings _settings = TestData.CreateParkSettings();
        private readonly Season _summer = TestData.CreateSeason(x => x.Name = "Zomer 2026");
        private readonly Season _spring = TestData.CreateSeason(x =>
        {
            x.Name = "Voorjaar 2026";
            x.StartDate = new DateOnly(2026, 4, 1);
            x.EndDate = new DateOnly(2026, 5, 31);
        });
        private readonly AccommodationType _type = TestData.CreateAccommodationType();
        private readonly AccommodationTypePrice _summerPrice;

        public ParkSettingsServiceTests()
        {
            _summerPrice = new AccommodationTypePrice { Id = Guid.NewGuid(), AccommodationTypeId = _type.Id, SeasonId = _summer.Id, PricePerNight = 120m };

            _settingsRepository.Setup(x => x.GetAllAsync()).ReturnsAsync([_settings]);
            _taxRepository.Setup(x => x.GetAllAsync()).ReturnsAsync([new TouristTaxRate { ValidFrom = new DateOnly(2026, 1, 1), AmountPerPersonPerNight = 2.5m }]);
            _seasonRepository.Setup(x => x.GetAllAsync()).ReturnsAsync([_summer, _spring]);
            _seasonRepository.Setup(x => x.GetByIdAsync(_summer.Id)).ReturnsAsync(_summer);
            _priceRepository.Setup(x => x.GetAllAsync()).ReturnsAsync([_summerPrice]);
            _typeRepository.Setup(x => x.GetBySlugAsync(_type.Slug)).ReturnsAsync(_type);

            _sut = new ParkSettingsService(_settingsRepository.Object, _taxRepository.Object, _seasonRepository.Object, _priceRepository.Object, _typeRepository.Object);
        }

        [Fact]
        public async Task UpdateAsync_ValidRules_SavesNewValues()
        {
            UpdateParkSettingsRequest request = _settings.ToRequest();
            request.HighSeasonMinimumNights = 5;
            request.PetCleaningFee = 40m;

            await _sut.UpdateAsync(request);

            Assert.Equal(5, _settings.HighSeasonMinimumNights);
            Assert.Equal(40m, _settings.PetCleaningFee);
            _settingsRepository.Verify(x => x.UpdateAsync(_settings), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_DepartureNotBeforeArrival_ThrowsArgumentExceptionAndSavesNothing()
        {
            UpdateParkSettingsRequest request = _settings.ToRequest();
            request.DepartureTime = new TimeOnly(16, 0);

            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() => _sut.UpdateAsync(request));

            Assert.Equal("De vertrektijd moet vóór de aankomsttijd liggen.", exception.Message);
            _settingsRepository.Verify(x => x.UpdateAsync(It.IsAny<ParkSettings>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_NoRefundDaysAboveFreeCancellationDays_ThrowsArgumentException()
        {
            UpdateParkSettingsRequest request = _settings.ToRequest();
            request.NoRefundDaysBeforeArrival = 40;

            await Assert.ThrowsAsync<ArgumentException>(() => _sut.UpdateAsync(request));
        }

        [Fact]
        public async Task AddTouristTaxRateAsync_SameValidFromAsExisting_ThrowsArgumentException()
        {
            AddTouristTaxRateRequest request = new AddTouristTaxRateRequest { AmountPerPersonPerNight = 3m, ValidFrom = new DateOnly(2026, 1, 1) };

            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() => _sut.AddTouristTaxRateAsync(request));

            Assert.Equal("Er is al een tarief met deze ingangsdatum.", exception.Message);
            _taxRepository.Verify(x => x.AddAsync(It.IsAny<TouristTaxRate>()), Times.Never);
        }

        [Fact]
        public async Task CreateSeasonAsync_OverlapsExistingSeason_ThrowsArgumentExceptionAndSavesNothing()
        {
            SaveSeasonRequest request = new SaveSeasonRequest { Name = "Herfst 2026", StartDate = new DateOnly(2026, 8, 15), EndDate = new DateOnly(2026, 9, 15) };

            await Assert.ThrowsAsync<ArgumentException>(() => _sut.CreateSeasonAsync(request));

            _seasonRepository.Verify(x => x.AddAsync(It.IsAny<Season>()), Times.Never);
        }

        [Fact]
        public async Task CreateSeasonAsync_SameNameAsExistingSeason_ThrowsArgumentException()
        {
            SaveSeasonRequest request = new SaveSeasonRequest { Name = "zomer 2026", StartDate = new DateOnly(2027, 7, 1), EndDate = new DateOnly(2027, 8, 31) };

            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() => _sut.CreateSeasonAsync(request));

            Assert.Equal("Er bestaat al een seizoen met deze naam.", exception.Message);
        }

        [Fact]
        public async Task UpdateSeasonAsync_KeepsOwnDates_SavesSeason()
        {
            SaveSeasonRequest request = new SaveSeasonRequest { Name = "Zomer 2026", StartDate = _summer.StartDate, EndDate = _summer.EndDate, IsHighSeason = true };

            await _sut.UpdateSeasonAsync(_summer.Id, request);

            Assert.True(_summer.IsHighSeason);
            _seasonRepository.Verify(x => x.UpdateAsync(_summer), Times.Once);
        }

        [Fact]
        public async Task DeleteSeasonAsync_UnknownSeason_ThrowsKeyNotFoundException()
        {
            await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.DeleteSeasonAsync(Guid.NewGuid()));
        }

        [Fact]
        public async Task UpdateTypePricesAsync_NewAndExistingSeasonPrice_AddsOneAndUpdatesOther()
        {
            UpdateTypePricesRequest request = new UpdateTypePricesRequest
            {
                DefaultPricePerNight = 90m,
                CleaningFee = 60m,
                SeasonPrices = [new SeasonPriceRequest { SeasonId = _summer.Id, PricePerNight = 150m }, new SeasonPriceRequest { SeasonId = _spring.Id, PricePerNight = 100m }],
            };

            await _sut.UpdateTypePricesAsync(_type.Slug, request);

            Assert.Equal(90m, _type.DefaultPricePerNight);
            Assert.Equal(150m, _summerPrice.PricePerNight);
            _priceRepository.Verify(x => x.UpdateAsync(_summerPrice), Times.Once);
            _priceRepository.Verify(x => x.AddAsync(It.Is<AccommodationTypePrice>(price => price.SeasonId == _spring.Id && price.PricePerNight == 100m)), Times.Once);
        }

        [Fact]
        public async Task UpdateTypePricesAsync_ClearsExistingSeasonPrice_ThrowsArgumentException()
        {
            UpdateTypePricesRequest request = new UpdateTypePricesRequest { DefaultPricePerNight = 90m, CleaningFee = 60m, SeasonPrices = [new SeasonPriceRequest { SeasonId = _summer.Id }] };

            await Assert.ThrowsAsync<ArgumentException>(() => _sut.UpdateTypePricesAsync(_type.Slug, request));

            _typeRepository.Verify(x => x.UpdateAsync(It.IsAny<AccommodationType>()), Times.Never);
        }

        [Fact]
        public async Task UpdateTypePricesAsync_UnknownType_ThrowsKeyNotFoundException()
        {
            UpdateTypePricesRequest request = new UpdateTypePricesRequest { DefaultPricePerNight = 90m, CleaningFee = 60m };

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.UpdateTypePricesAsync("bestaat-niet", request));
        }
    }
}
