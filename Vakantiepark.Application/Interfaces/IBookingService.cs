using Vakantiepark.Shared.DTOs.Booking;

namespace Vakantiepark.Application.Interfaces
{
    public interface IBookingService
    {
        // Checks the stay and returns what the summary shows, without saving anything.
        Task<BookingQuoteResponse> GetQuoteAsync(string accommodationTypeSlug, DateOnly arrivalDate, DateOnly departureDate, int numberOfGuests, string? preferredZone);

        Task<BookingResponse> CreateAsync(CreateBookingRequest request);
    }
}
