using Vakantiepark.Domain.Entities;
using Vakantiepark.Domain.Filters;
using Vakantiepark.Domain.Paging;

namespace Vakantiepark.Domain.Interfaces.Repositories
{
    public interface IBookingRepository : IRepository<Booking>
    {
        Task<Booking?> GetByBookingNumberAsync(string bookingNumber);
        Task<bool> ExistsByBookingNumberIncludingDeletedAsync(string bookingNumber);
        Task<bool> TryAddAsync(Booking booking);

        // US-12: when a booking is changed it is left out, otherwise a guest who shifts his stay would block himself.
        Task<List<Guid>> GetUnitIdsWithOverlappingBookingAsync(List<Guid> unitIds, DateOnly arrivalDate, DateOnly departureDate, Guid? excludedBookingId = null);

        Task<List<Booking>> GetByUnitIdInPeriodAsync(Guid unitId, DateOnly startDate, DateOnly endDate);
        Task<Booking?> GetByAccessTokenAsync(string accessToken);
        Task<Booking?> GetByIdWithDetailsAsync(Guid id);
        Task CancelAsync(Booking booking, decimal cancellationFee);

        Task<bool> TryUpdateAsync(Booking booking, List<BookingGuest> coTravellers);

        // US-13: one page of the reservations overview. Cancelled bookings are included;
        // today decides which bookings count as completed.
        Task<PagedResult<Booking>> SearchAsync(BookingSearchFilter filter, DateOnly today, int page, int pageSize);

        // US-16: the active bookings of these units whose stay includes the night of this date.
        Task<List<Booking>> GetStayingOnDateAsync(List<Guid> unitIds, DateOnly date);

        // US-15: the bookings that arrive or depart on this day, with unit, type and zone. Cancelled ones are left out.
        Task<List<Booking>> GetArrivingOrDepartingOnAsync(DateOnly date);
    }
}
