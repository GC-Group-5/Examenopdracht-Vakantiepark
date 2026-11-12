using Vakantiepark.Shared.DTOs.Booking;
using Vakantiepark.Shared.DTOs.Price;
using Vakantiepark.Domain.Entities;

namespace Vakantiepark.Application.Extensions
{
    public static class BookingExtensions
    {
        public static Booking ToEntity(this CreateBookingRequest request, Guid accommodationUnitId, string bookingNumber, PriceSpecificationResponse price)
            => new Booking()
            {
                BookingNumber = bookingNumber,
                Name = request.Name,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                ArrivalDate = request.ArrivalDate,
                DepartureDate = request.DepartureDate,
                ArrivalTime = request.ArrivalTime,
                DepartureTime = request.DepartureTime,
                NumberOfGuests = request.NumberOfGuests,
                AccommodationPrice = price.AccommodationPrice,
                CleaningFee = price.CleaningFee,
                TouristTax = price.TouristTax,
                TotalPrice = price.TotalPrice,
                AccommodationUnitId = accommodationUnitId
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
    }
}
