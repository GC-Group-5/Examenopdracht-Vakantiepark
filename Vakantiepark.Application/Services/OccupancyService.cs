using Vakantiepark.Application.Extensions;
using Vakantiepark.Application.Interfaces;
using Vakantiepark.Application.Validations.AccommodationSearch;
using Vakantiepark.Application.Validations.Paging;
using Vakantiepark.Domain.Entities;
using Vakantiepark.Domain.Interfaces.Repositories;
using Vakantiepark.Shared.Constants;
using Vakantiepark.Shared.DTOs;
using Vakantiepark.Shared.DTOs.Occupancy;

namespace Vakantiepark.Application.Services
{
    // US-16: the status of every unit (beschikbaar, bezet, niet verhuurbaar) on one date.
    public class OccupancyService : IOccupancyService
    {
        private readonly IAccommodationUnitRepository UnitRepository;
        private readonly IBookingRepository BookingRepository;
        private readonly TimeProvider TimeProvider;

        public OccupancyService(IAccommodationUnitRepository unitRepository, IBookingRepository bookingRepository, TimeProvider timeProvider)
        {
            UnitRepository = unitRepository;
            BookingRepository = bookingRepository;
            TimeProvider = timeProvider;
        }

        // The counts need every unit, so the page is cut here instead of in the database (about 200 units).
        public async Task<OccupancyResponse> GetAsync(OccupancyRequest request)
        {
            request.ValidatePaging();
            DateOnly date = request.Date ?? TimeProvider.GetToday();

            List<Accommodation> units = await UnitRepository.GetForOccupancyAsync(date, request.AccommodationTypeSlug, request.ZoneId, AccommodationSearchValidation.ParseCategory(request.Category));
            List<Booking> bookings = await BookingRepository.GetStayingOnDateAsync(units.Select(unit => unit.Id).ToList(), date);

            List<OccupancyUnitResponse> rows = units
                .Select(unit => unit.ToOccupancyUnit(bookings.FirstOrDefault(booking => booking.AccommodationUnitId == unit.Id)))
                .ToList();

            return new OccupancyResponse()
            {
                Date = date,
                AvailableCount = rows.Count(row => row.Status == OccupancyStatuses.Beschikbaar),
                OccupiedCount = rows.Count(row => row.Status == OccupancyStatuses.Bezet),
                OutOfServiceCount = rows.Count(row => row.Status == OccupancyStatuses.NietVerhuurbaar),
                Units = new PagedResponse<OccupancyUnitResponse>()
                {
                    Items = rows.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToList(),
                    Page = request.Page,
                    PageSize = request.PageSize,
                    TotalCount = rows.Count,
                },
            };
        }
    }
}
