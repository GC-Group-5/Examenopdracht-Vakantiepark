using Vakantiepark.Domain.Enums;

namespace Vakantiepark.Domain.Filters
{
    // What a staff member searches and filters on in the reservations overview (US-13).
    // Every field is optional; an empty filter returns all bookings, cancelled ones included.
    public class BookingSearchFilter
    {
        // Part of the booking number, the main booker's name or email, case-insensitive.
        public string? Search { get; set; }

        // Bookings with at least one day inside this period (the departure day counts).
        public DateOnly? From { get; set; }
        public DateOnly? To { get; set; }

        public BookingState? State { get; set; }
        public string? AccommodationTypeSlug { get; set; }
    }
}
