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
        Task<Booking?> GetByBookingNumberAndEmailAsync(string bookingNumber, string email);
    }
}
