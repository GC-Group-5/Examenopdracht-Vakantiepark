using Vakantiepark.Application.Interfaces;
using Vakantiepark.Application.Validations.AccommodationSearch;
using Vakantiepark.Domain.Entities;
using Vakantiepark.Domain.Interfaces.Repositories;
using Vakantiepark.Shared.DTOs.Accommodation;
using Vakantiepark.Shared.DTOs.Price;

namespace Vakantiepark.Application.Services
{
    // US-06: find bookable stays. Availability is per physical unit: the unit must be
    // free for the whole period (no overlapping active booking; a departure day and an
    // arrival day may be the same) and must fit the party size, pets and preferred zone.
    // Zones belong to one category and never change the price. The price is taken from
    // the price service so every screen shows the same amount.
    public class AccommodationSearchService : IAccommodationSearchService
    {
        private readonly IAccommodationUnitRepository UnitRepository;
        private readonly IAccommodationTypeRepository TypeRepository;
        private readonly IRepository<AccommodationType> TypeListRepository;
        private readonly IBookingRepository BookingRepository;
        private readonly IRepository<Season> SeasonRepository;
        private readonly IRepository<ParkSettings> ParkSettingsRepository;
        private readonly IRepository<Zone> ZoneRepository;
        private readonly IPriceService PriceService;

        public AccommodationSearchService(
            IAccommodationUnitRepository unitRepository,
            IAccommodationTypeRepository typeRepository,
            IRepository<AccommodationType> typeListRepository,
            IBookingRepository bookingRepository,
            IRepository<Season> seasonRepository,
            IRepository<ParkSettings> parkSettingsRepository,
            IRepository<Zone> zoneRepository,
            IPriceService priceService)
        {
            UnitRepository = unitRepository;
            TypeRepository = typeRepository;
            TypeListRepository = typeListRepository;
            BookingRepository = bookingRepository;
            SeasonRepository = seasonRepository;
            ParkSettingsRepository = parkSettingsRepository;
            ZoneRepository = zoneRepository;
            PriceService = priceService;
        }

        public async Task<AccommodationSearchResponse> SearchAsync(string? accommodationTypeSlug, DateOnly arrivalDate, DateOnly departureDate, int numberOfGuests, int numberOfPets, Guid? zoneId)
        {
            arrivalDate.ValidateSearchPeriod(departureDate, numberOfGuests, numberOfPets);

            List<Season> seasons = (await SeasonRepository.GetAllAsync()).ToList();
            ParkSettings? settings = (await ParkSettingsRepository.GetAllAsync()).FirstOrDefault();

            string? highSeasonMessage = seasons.GetHighSeasonSearchMessage(settings, arrivalDate, departureDate);
            if (highSeasonMessage is not null)
                return new AccommodationSearchResponse { Message = highSeasonMessage };

            List<AccommodationType> types = (await TypeListRepository.GetAllAsync()).ToList();

            if (!string.IsNullOrWhiteSpace(accommodationTypeSlug))
            {
                AccommodationType? searchedType = await TypeRepository.GetBySlugAsync(accommodationTypeSlug);
                types = searchedType is null ? [] : [searchedType];
            }

            if (zoneId is not null)
            {
                Zone? zone = await ZoneRepository.GetByIdAsync(zoneId.Value);
                if (zone is null)
                    throw new ArgumentException("De gekozen zone is niet gevonden. Kies een andere zone.");
            }

            List<Accommodation> units = await UnitRepository.GetAllWithDetailsAsync();
            List<Guid> allUnitIds = units.Select(unit => unit.Id).ToList();
            List<Guid> bookedUnitIds = await BookingRepository.GetUnitIdsWithOverlappingBookingAsync(allUnitIds, arrivalDate, departureDate);

            List<AccommodationTypeAvailability> results = [];

            foreach (AccommodationType type in types)
            {
                List<Accommodation> freeUnits = units
                    .Where(unit => unit.TypeId == type.Id)
                    .Where(unit => unit.MaxPersons >= numberOfGuests)
                    .Where(unit => numberOfPets == 0 || (unit.GetNumber("maxPets") ?? 0) >= numberOfPets)
                    .Where(unit => zoneId is null || unit.ZoneId == zoneId)
                    .Where(unit => !bookedUnitIds.Contains(unit.Id))
                    .ToList();

                if (freeUnits.Count == 0)
                    continue;

                PriceSpecificationResponse price = await PriceService.CalculateAsync(type.Slug, arrivalDate, departureDate, numberOfGuests);

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
