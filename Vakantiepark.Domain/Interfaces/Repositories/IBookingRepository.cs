using Vakantiepark.Domain.Entities;

namespace Vakantiepark.Domain.Interfaces.Repositories
{
    public interface IBookingRepository : IRepository<Booking>
    {
        Task<bool> ExistsByBookingNumberIncludingDeletedAsync(string bookingNumber);
        Task<bool> TryAddAsync(Booking booking);
        Task<List<Guid>> GetUnitIdsWithOverlappingBookingAsync(List<Guid> unitIds, DateOnly arrivalDate, DateOnly departureDate);
    }
}
