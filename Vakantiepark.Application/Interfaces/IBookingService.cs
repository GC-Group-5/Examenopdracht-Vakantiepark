using Vakantiepark.Domain.Filters;
using Vakantiepark.Shared.DTOs.Booking;
using Vakantiepark.Shared.DTOs.Reservations;

namespace Vakantiepark.Application.Interfaces
{
    public interface IBookingService
    {
        // Checks the stay and returns what the summary shows, without saving anything. The page counts the
        // children under 12 from the birth dates filled in so far; CreateAsync counts them again itself.
        Task<BookingQuoteResponse> GetQuoteAsync(string accommodationTypeSlug, DateOnly arrivalDate, DateOnly departureDate, AccommodationUnitFilter filter, int numberOfChildrenUnder12, string? preferredZone);

        Task<BookingResponse> CreateAsync(CreateBookingRequest request);
        Task<MyBookingResponse> GetMineByTokenAsync(string token);
        Task<MyBookingResponse> GetMineAsync(Guid bookingId);

        // US-12: cancel or change the guest's own booking. guestBookingId comes from the guest login
        // token; without it the request must carry the code from the email link.
        Task<CancellationQuoteResponse> GetCancellationQuoteAsync(BookingAccessRequest access, Guid? guestBookingId);
        Task<MyBookingResponse> CancelAsync(BookingAccessRequest access, Guid? guestBookingId);
        Task<ChangeQuoteResponse> GetChangeQuoteAsync(ChangeBookingRequest request, Guid? guestBookingId);
        Task<MyBookingResponse> ChangeAsync(ChangeBookingRequest request, Guid? guestBookingId);

        // US-14: the same by staff, without the guest's term; staffUserId goes into the change log.
        Task ChangeByStaffAsync(Guid bookingId, StaffChangeBookingRequest request, Guid staffUserId);
        // Explained to the employee ("de boeker krijgt..."), for a medewerker and a beheerder alike.
        Task<CancellationQuoteResponse> GetCancellationQuoteForStaffAsync(Guid bookingId);
        Task CancelByStaffAsync(Guid bookingId, StaffCancelBookingRequest request, Guid staffUserId);
    }
}
