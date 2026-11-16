using Vakantiepark.Domain.Filters;
using Vakantiepark.Shared.DTOs.Booking;

namespace Vakantiepark.Application.Interfaces
{
    public interface IBookingService
    {
        Task<BookingQuoteResponse> GetQuoteAsync(string accommodationTypeSlug, DateOnly arrivalDate, DateOnly departureDate, AccommodationUnitFilter filter, string? preferredZone);

        Task<BookingResponse> CreateAsync(CreateBookingRequest request);
        Task<MyBookingResponse> GetMineByTokenAsync(string token);
        Task<MyBookingResponse> GetMineByNumberAndEmailAsync(string bookingNumber, string email);

        Task<CancellationQuoteResponse> GetCancellationQuoteAsync(BookingAccessRequest access);
        Task<MyBookingResponse> CancelAsync(BookingAccessRequest access);

        // US-12: the old and new price of a change, and the change itself.
        Task<ChangeQuoteResponse> GetChangeQuoteAsync(ChangeBookingRequest request);
        Task<MyBookingResponse> ChangeAsync(ChangeBookingRequest request);
    }
}
