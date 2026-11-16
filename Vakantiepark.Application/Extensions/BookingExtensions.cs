using Vakantiepark.Shared.DTOs.Booking;
using Vakantiepark.Shared.DTOs.Price;
using Vakantiepark.Domain.Entities;
using System.Security.Cryptography;

namespace Vakantiepark.Application.Extensions
{
    public static class BookingExtensions
    {
        // Everyone who stays becomes a BookingGuest, the main booker first.
        public static Booking ToEntity(this CreateBookingRequest request, Guid accommodationUnitId, string bookingNumber, PriceSpecificationResponse price, ParkSettings settings)
            => new Booking()
            {
                BookingNumber = bookingNumber,
                AccessToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant(),
                Name = request.Name.Trim(),
                Email = request.Email.Trim(),
                PhoneNumber = request.PhoneNumber.Trim(),
                ArrivalDate = request.ArrivalDate,
                DepartureDate = request.DepartureDate,
                ArrivalTime = settings.ArrivalTime,
                DepartureTime = settings.DepartureTime,
                NumberOfGuests = request.NumberOfGuests,
                NumberOfPets = request.NumberOfPets,
                AccommodationPrice = price.AccommodationPrice,
                CleaningFee = price.CleaningFee,
                TouristTax = price.TouristTax,
                TotalPrice = price.TotalPrice,
                Deposit = price.Deposit,
                AccommodationUnitId = accommodationUnitId,
                Guests = new List<BookingGuest>()
                    {
                        new BookingGuest() { Name = request.Name.Trim(), BirthDate = request.BirthDate!.Value, IsMainBooker = true }
                    }
                    .Concat(request.CoTravellers.Select(traveller => new BookingGuest()
                    {
                        Name = traveller.Name.Trim(),
                        BirthDate = traveller.BirthDate!.Value,
                    }))
                    .ToList(),
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
                NumberOfPets = booking.NumberOfPets,
                AccommodationPrice = booking.AccommodationPrice,
                CleaningFee = booking.CleaningFee,
                TouristTax = booking.TouristTax,
                TotalPrice = booking.TotalPrice,
                Deposit = booking.Deposit,
            };
    }
}
