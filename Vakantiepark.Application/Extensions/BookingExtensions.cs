using Vakantiepark.Shared.DTOs.Booking;
using Vakantiepark.Shared.DTOs.Price;
using Vakantiepark.Domain.Entities;

namespace Vakantiepark.Application.Extensions
{
    public static class BookingExtensions
    {
        // Arrival and departure times come from the park settings, not from the guest.
        public static Booking ToEntity(this CreateBookingRequest request, Guid accommodationUnitId, string bookingNumber, PriceSpecificationResponse price, ParkSettings settings)
            => new Booking()
            {
                BookingNumber = bookingNumber,
                Name = request.Name,
                BirthDate = request.BirthDate!.Value,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                ArrivalDate = request.ArrivalDate,
                DepartureDate = request.DepartureDate,
                ArrivalTime = settings.ArrivalTime,
                DepartureTime = settings.DepartureTime,
                NumberOfGuests = request.NumberOfGuests,
                AccommodationPrice = price.AccommodationPrice,
                CleaningFee = price.CleaningFee,
                TouristTax = price.TouristTax,
                TotalPrice = price.TotalPrice,
                AccommodationUnitId = accommodationUnitId,
                Travelers = request.Travelers
                    .Select(traveler => new BookingTraveler()
                    {
                        Name = traveler.Name.Trim(),
                        BirthDate = traveler.BirthDate!.Value
                    })
                    .ToList()
            };

        public static BookingResponse ToResponse(this Booking booking)
            => new BookingResponse()
            {
                BookingNumber = booking.BookingNumber,
                Name = booking.Name,
                Email = booking.Email,
                PhoneNumber = booking.PhoneNumber,
                ArrivalDate = booking.ArrivalDate,
                DepartureDate = booking.DepartureDate,
                ArrivalTime = booking.ArrivalTime,
                DepartureTime = booking.DepartureTime,
                NumberOfGuests = booking.NumberOfGuests,
                AccommodationPrice = booking.AccommodationPrice,
                CleaningFee = booking.CleaningFee,
                TouristTax = booking.TouristTax,
                TotalPrice = booking.TotalPrice
            };

        public static BookingInfoResponse ToInfoResponse(this ParkSettings settings, string accommodationTypeName, List<string> zones)
            => new BookingInfoResponse()
            {
                AccommodationTypeName = accommodationTypeName,
                Zones = zones,
                ArrivalTime = settings.ArrivalTime,
                DepartureTime = settings.DepartureTime,
                DepositPercentage = settings.DepositPercentage,
                FreeCancellationDaysBeforeArrival = settings.FreeCancellationDaysBeforeArrival,
                NoRefundDaysBeforeArrival = settings.NoRefundDaysBeforeArrival
            };
    }
}
