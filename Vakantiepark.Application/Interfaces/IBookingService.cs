using Vakantiepark.Domain.Filters;
using Vakantiepark.Shared.DTOs.Booking;
using Vakantiepark.Shared.DTOs.Reservations;

namespace Vakantiepark.Application.Interfaces
{
    public interface IBookingService
    {
        // Checks the stay and returns what the summary shows, without saving anything.
        Task<BookingQuoteResponse> GetQuoteAsync(string accommodationTypeSlug, DateOnly arrivalDate, DateOnly departureDate, AccommodationUnitFilter filter, int numberOfChildrenUnder12, string? preferredZone);

        Task<BookingResponse> CreateAsync(CreateBookingRequest request);
        Task<MyBookingResponse> GetMineByTokenAsync(string token);
        Task<MyBookingResponse> GetMineAsync(Guid bookingId);


        Task<CancellationQuoteResponse> GetCancellationQuoteAsync(BookingAccessRequest access, Guid? guestBookingId);
        Task<MyBookingResponse> CancelAsync(BookingAccessRequest access, Guid? guestBookingId);
        Task<ChangeQuoteResponse> GetChangeQuoteAsync(ChangeBookingRequest request, Guid? guestBookingId);
        Task<MyBookingResponse> ChangeAsync(ChangeBookingRequest request, Guid? guestBookingId);

        Task ChangeByStaffAsync(Guid bookingId, StaffChangeBookingRequest request, Guid staffUserId);

        Task<CancellationQuoteResponse> GetCancellationQuoteForStaffAsync(Guid bookingId);
        Task CancelByStaffAsync(Guid bookingId, StaffCancelBookingRequest request, Guid staffUserId);
    }
}
