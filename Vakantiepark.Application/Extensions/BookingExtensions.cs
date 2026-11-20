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
                WheelchairAccessible = request.WheelchairAccessible,
                AccommodationPrice = price.AccommodationPrice,
                CleaningFee = price.CleaningFee,
                PetCleaningFee = price.PetCleaningFee,
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
            => booking.ToResponse<BookingResponse>();

        // Shared by every booking response, so a new field never has to be added twice.
        public static T ToResponse<T>(this Booking booking) where T : BookingResponse, new()
            => new T()
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
                WheelchairAccessible = booking.WheelchairAccessible,
                AccommodationPrice = booking.AccommodationPrice,
                CleaningFee = booking.CleaningFee,
                PetCleaningFee = booking.PetCleaningFee,
                TouristTax = booking.TouristTax,
                TotalPrice = booking.TotalPrice,
                Deposit = booking.Deposit,
            };

        // Needs Guests and AccommodationUnit with Type and Zone loaded.
        public static MyBookingResponse ToMyBookingResponse(this Booking booking, string status)
        {
            MyBookingResponse response = booking.ToResponse<MyBookingResponse>();
            response.AccommodationTypeName = booking.AccommodationUnit.Type.Name;
            response.Zone = booking.AccommodationUnit.Zone.Name;
            response.Status = status;
            response.Guests = booking.Guests
                .OrderByDescending(guest => guest.IsMainBooker)
                .Select(guest => new GuestResponse()
                {
                    Name = guest.Name,
                    BirthDate = guest.BirthDate,
                    IsMainBooker = guest.IsMainBooker,
                })
                .ToList();

            return response;
        }
    }
}
