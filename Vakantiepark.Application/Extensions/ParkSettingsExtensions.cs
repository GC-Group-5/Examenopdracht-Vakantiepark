using Vakantiepark.Domain.Entities;
using Vakantiepark.Shared.DTOs.Park;

namespace Vakantiepark.Application.Extensions
{
    public static class ParkSettingsExtensions
    {
        public static UpdateParkSettingsRequest ToRequest(this ParkSettings settings) => new()
        {
            ArrivalTime = settings.ArrivalTime,
            DepartureTime = settings.DepartureTime,
            HighSeasonMinimumNights = settings.HighSeasonMinimumNights,
            DepositPercentage = settings.DepositPercentage,
            FreeCancellationDaysBeforeArrival = settings.FreeCancellationDaysBeforeArrival,
            NoRefundDaysBeforeArrival = settings.NoRefundDaysBeforeArrival,
            PetCleaningFee = settings.PetCleaningFee,
        };

        // Called after model validation, so every required value is filled in.
        public static void ApplyTo(this UpdateParkSettingsRequest request, ParkSettings settings)
        {
            settings.ArrivalTime = request.ArrivalTime!.Value;
            settings.DepartureTime = request.DepartureTime!.Value;
            settings.HighSeasonMinimumNights = request.HighSeasonMinimumNights!.Value;
            settings.DepositPercentage = request.DepositPercentage!.Value;
            settings.FreeCancellationDaysBeforeArrival = request.FreeCancellationDaysBeforeArrival!.Value;
            settings.NoRefundDaysBeforeArrival = request.NoRefundDaysBeforeArrival!.Value;
            settings.PetCleaningFee = request.PetCleaningFee!.Value;
        }

        public static AddTouristTaxRateRequest ToRequest(this TouristTaxRate rate) => new()
        {
            AmountPerPersonPerNight = rate.AmountPerPersonPerNight,
            ValidFrom = rate.ValidFrom,
        };

        public static SeasonResponse ToResponse(this Season season) => new()
        {
            Id = season.Id,
            Name = season.Name,
            StartDate = season.StartDate,
            EndDate = season.EndDate,
            IsHighSeason = season.IsHighSeason,
        };

        public static void ApplyTo(this SaveSeasonRequest request, Season season)
        {
            season.Name = request.Name.Trim();
            season.StartDate = request.StartDate!.Value;
            season.EndDate = request.EndDate!.Value;
            season.IsHighSeason = request.IsHighSeason;
        }

        public static TypePricesResponse ToPricesResponse(this AccommodationType type, IEnumerable<Season> seasons, IEnumerable<AccommodationTypePrice> prices) => new()
        {
            Slug = type.Slug,
            Name = type.Name,
            DefaultPricePerNight = type.DefaultPricePerNight,
            CleaningFee = type.CleaningFee,
            SeasonPrices = seasons
                .Select(season => new SeasonPriceRequest
                {
                    SeasonId = season.Id,
                    PricePerNight = prices.FirstOrDefault(price => price.AccommodationTypeId == type.Id && price.SeasonId == season.Id)?.PricePerNight,
                })
                .ToList(),
        };
    }
}
