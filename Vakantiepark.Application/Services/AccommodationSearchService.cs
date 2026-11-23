using Vakantiepark.Application.Interfaces;
using Vakantiepark.Application.Validations.AccommodationSearch;
using Vakantiepark.Domain.Entities;
using Vakantiepark.Domain.Enums;
using Vakantiepark.Domain.Filters;
using Vakantiepark.Domain.Interfaces.Repositories;
using Vakantiepark.Shared.DTOs.Accommodation;
using Vakantiepark.Shared.DTOs.Price;

namespace Vakantiepark.Application.Services
{
    // US-06: find bookable stays. Availability is per physical unit: the unit must be
    // free for the whole period (no overlapping active booking; a departure day and an
    // arrival day may be the same) and must fit the party (AccommodationUnitFilter, the
    // same rule the booking uses), the category and the preferred zone.
    // Zones belong to one category and never change the price. The price is taken from
    // the price service so every screen shows the same amount.
    public class AccommodationSearchService : IAccommodationSearchService
    {
        private readonly IAccommodationUnitRepository UnitRepository;
        private readonly IAccommodationTypeRepository TypeRepository;
        private readonly IBookingRepository BookingRepository;
        private readonly IRepository<Season> SeasonRepository;
        private readonly IRepository<ParkSettings> ParkSettingsRepository;
        private readonly IRepository<Zone> ZoneRepository;
        private readonly IRepository<MaintenancePeriod> MaintenanceRepository;
        private readonly IPriceService PriceService;

        public AccommodationSearchService(
            IAccommodationUnitRepository unitRepository,
            IAccommodationTypeRepository typeRepository,
            IBookingRepository bookingRepository,
            IRepository<Season> seasonRepository,
            IRepository<ParkSettings> parkSettingsRepository,
            IRepository<Zone> zoneRepository,
            IRepository<MaintenancePeriod> maintenanceRepository,
            IPriceService priceService)
        {
            UnitRepository = unitRepository;
            TypeRepository = typeRepository;
            BookingRepository = bookingRepository;
            SeasonRepository = seasonRepository;
            ParkSettingsRepository = parkSettingsRepository;
            ZoneRepository = zoneRepository;
            MaintenanceRepository = maintenanceRepository;
            PriceService = priceService;
        }

        public async Task<AccommodationSearchResponse> SearchAsync(string? accommodationTypeSlug, string? category, DateOnly arrivalDate, DateOnly departureDate, AccommodationUnitFilter filter, Guid? zoneId)
        {
            arrivalDate.ValidateSearchPeriod(departureDate, filter.NumberOfGuests, filter.NumberOfPets);
            AccommodationCategory? searchedCategory = AccommodationSearchValidation.ParseCategory(category);

            List<Season> seasons = (await SeasonRepository.GetAllAsync()).ToList();
            ParkSettings? settings = (await ParkSettingsRepository.GetAllAsync()).FirstOrDefault();

            string? highSeasonMessage = seasons.GetHighSeasonSearchMessage(settings, arrivalDate, departureDate);
            if (highSeasonMessage is not null)
                return new AccommodationSearchResponse { Message = highSeasonMessage };

            List<AccommodationType> types = (await TypeRepository.GetAllAsync()).ToList();

            if (!string.IsNullOrWhiteSpace(accommodationTypeSlug))
            {
                AccommodationType? searchedType = await TypeRepository.GetBySlugAsync(accommodationTypeSlug);
                types = searchedType is null ? [] : [searchedType];
            }

            if (searchedCategory is not null)
                types = types.Where(type => type.Category == searchedCategory).ToList();

            if (zoneId is not null)
            {
                Zone? zone = await ZoneRepository.GetByIdAsync(zoneId.Value);
                if (zone is null)
                    throw new ArgumentException("De gekozen zone is niet gevonden. Kies een andere zone.");

                // Criterion: only zones of the chosen type's category are offered.
                if (types.Count == 1 && zone.Category != types[0].Category)
                    throw new ArgumentException("Kies een zone die bij het geselecteerde verblijfstype hoort.");

                if (searchedCategory is not null && zone.Category != searchedCategory)
                    throw new ArgumentException("Kies een zone die bij de gekozen soort verblijf hoort.");
            }

            List<Accommodation> units = await UnitRepository.GetAllWithDetailsAsync();
            List<Guid> allUnitIds = units.Select(unit => unit.Id).ToList();
            List<Guid> bookedUnitIds = await BookingRepository.GetUnitIdsWithOverlappingBookingAsync(allUnitIds, arrivalDate, departureDate);

            // Units out of service (US-17) for any night of the stay never count as free.
            List<Guid> maintenanceUnitIds = (await MaintenanceRepository.GetAllAsync())
                .Where(period => period.StartDate < departureDate && period.EndDate >= arrivalDate)
                .Select(period => period.AccommodationUnitId)
                .Distinct()
                .ToList();

            List<AccommodationTypeAvailability> results = [];

            foreach (AccommodationType type in types)
            {
                List<Accommodation> freeUnits = units
                    .Where(unit => unit.TypeId == type.Id)
                    .Where(filter.Matches)
                    .Where(unit => zoneId is null || unit.ZoneId == zoneId)
                    .Where(unit => !bookedUnitIds.Contains(unit.Id))
                    .Where(unit => !maintenanceUnitIds.Contains(unit.Id))
                    .ToList();

                if (freeUnits.Count == 0)
                    continue;

                // The search does not ask for children under 12 yet, so everyone counts for the tourist tax.
                PriceSpecificationResponse price = await PriceService.CalculateAsync(type.Slug, arrivalDate, departureDate, filter.NumberOfGuests, 0, filter.NumberOfPets);

                results.Add(new AccommodationTypeAvailability
                {
                    Slug = type.Slug,
                    Name = type.Name,
                    Category = type.Category.ToString(),
                    MaxPersons = freeUnits.Max(unit => unit.MaxPersons),
                    AvailableUnitCount = freeUnits.Count,
                    TotalPrice = price.TotalPrice,
                    Zones = freeUnits
                        .GroupBy(unit => unit.ZoneId)
                        .Select(group => new ZoneAvailability
                        {
                            Id = group.Key,
                            Name = units.First(unit => unit.ZoneId == group.Key).Zone.Name,
                            AvailableUnitCount = group.Count(),
                        })
                        .ToList(),
                });
            }

            results = results.OrderBy(result => result.TotalPrice).ToList();

            return new AccommodationSearchResponse
            {
                Results = results,
                Message = results.Count == 0 ? BuildNoResultsMessage(zoneId is not null) : null,
            };
        }

        private static string BuildNoResultsMessage(bool zoneRequested)
        {
            if (zoneRequested)
                return "In de gekozen zone is geen vrij verblijf voor deze periode. Kies een andere zone of probeer andere data.";

            return "Geen vrije verblijven voor deze periode. Probeer andere data.";
        }
    }
}
