using Vakantiepark.Domain.Entities;

namespace Vakantiepark.Domain.Interfaces.Repositories
{
    public interface IBookingRepository : IRepository<Booking>
    {
        Task<Booking?> GetByBookingNumberAsync(string bookingNumber);
        Task<bool> ExistsByBookingNumberIncludingDeletedAsync(string bookingNumber);
        Task<bool> TryAddAsync(Booking booking);
        Task<List<Guid>> GetUnitIdsWithOverlappingBookingAsync(List<Guid> unitIds, DateOnly arrivalDate, DateOnly departureDate);
        Task<List<Booking>> GetByUnitIdInPeriodAsync(Guid unitId, DateOnly startDate, DateOnly endDate);
        Task<Booking?> GetByAccessTokenAsync(string accessToken);
        Task<Booking?> GetByIdWithDetailsAsync(Guid id);
        Task CancelAsync(Booking booking, decimal cancellationFee);

        // US-12: changing a booking. The booking itself is left out of the overlap check,
        // otherwise a guest who shifts his stay would block himself.
        Task<List<Guid>> GetUnitIdsWithOverlappingBookingExcludingAsync(List<Guid> unitIds, DateOnly arrivalDate, DateOnly departureDate, Guid excludedBookingId);
        Task<bool> TryUpdateAsync(Booking booking, List<BookingGuest> coTravellers);
    }
}
