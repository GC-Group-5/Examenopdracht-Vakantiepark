using Vakantiepark.Domain.Entities;

namespace Vakantiepark.Domain.Interfaces.Repositories
{
    public interface IBookingRepository : IRepository<Booking>
    {
        Task<Booking?> GetByBookingNumberAsync(string bookingNumber);
        Task<List<Guid>> GetUnitIdsWithOverlappingBookingAsync(List<Guid> unitIds, DateOnly arrivalDate, DateOnly departureDate);
    }
}
