using Vakantiepark.Shared.DTOs.Booking;
using Vakantiepark.Shared.DTOs.Price;
using Vakantiepark.Shared.DTOs.Reservations;
using Vakantiepark.Domain.Entities;
using System.Security.Cryptography;

namespace Vakantiepark.Application.Extensions
{
    public static class BookingExtensions
    {
        // The main booker and every fellow traveller with a name become a BookingGuest.
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
                NumberOfChildrenUnder12 = request.NumberOfChildrenUnder12,
                NumberOfPets = request.NumberOfPets,
                WheelchairAccessible = request.WheelchairAccessible,
                AccommodationPrice = price.AccommodationPrice,
                CleaningFee = price.CleaningFee,
                PetCleaningFee = price.PetCleaningFee,
                TouristTax = price.TouristTax,
                TotalPrice = price.TotalPrice,
                Deposit = price.Deposit,
                AccommodationUnitId = accommodationUnitId,
                Guests =
                [
                    new BookingGuest() { Name = request.Name.Trim(), BirthDate = request.BirthDate!.Value, IsMainBooker = true },
                    .. request.CoTravellers.ToGuests(),
                ],
            };

        // Only travellers with a name are saved; an empty row means "not known yet".
        public static List<BookingGuest> ToGuests(this List<TravellerRequest> coTravellers)
            => coTravellers
                .Where(traveller => !string.IsNullOrWhiteSpace(traveller.Name))
                .Select(traveller => new BookingGuest() { Name = traveller.Name!.Trim(), BirthDate = traveller.BirthDate })
                .ToList();

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
                NumberOfChildrenUnder12 = booking.NumberOfChildrenUnder12,
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
            => booking.ToMyBookingResponse<MyBookingResponse>(status);

        // Generic so the staff detail page (US-13) reuses everything the guest sees.
        public static T ToMyBookingResponse<T>(this Booking booking, string status) where T : MyBookingResponse, new()
        {
            T response = booking.ToResponse<T>();
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

        // Needs AccommodationUnit with Type and Zone loaded. Reception walks the park zone by zone,
        // so both lists come sorted by zone and then unit.
        public static DayOverviewResponse ToDayOverview(this List<Booking> bookings, DateOnly date)
            => new DayOverviewResponse()
            {
                Date = date,
                Arrivals = bookings.Where(booking => booking.ArrivalDate == date).ToDayOverviewItems(),
                Departures = bookings.Where(booking => booking.DepartureDate == date).ToDayOverviewItems(),
            };

        private static List<DayOverviewItemResponse> ToDayOverviewItems(this IEnumerable<Booking> bookings)
            => bookings
                .OrderBy(booking => booking.AccommodationUnit.Zone.Name)
                .ThenBy(booking => booking.AccommodationUnit.Code)
                .Select(booking => new DayOverviewItemResponse()
                {
                    Id = booking.Id,
                    BookingNumber = booking.BookingNumber,
                    Name = booking.Name,
                    AccommodationTypeName = booking.AccommodationUnit.Type.Name,
                    UnitCode = booking.AccommodationUnit.Code,
                    Zone = booking.AccommodationUnit.Zone.Name,
                    NumberOfGuests = booking.NumberOfGuests,
                    NumberOfPets = booking.NumberOfPets,
                })
                .ToList();

        // Needs AccommodationUnit with Type loaded.
        public static ReservationListItemResponse ToReservationListItem(this Booking booking, string status)
            => new ReservationListItemResponse()
            {
                Id = booking.Id,
                BookingNumber = booking.BookingNumber,
                Name = booking.Name,
                Email = booking.Email,
                AccommodationTypeName = booking.AccommodationUnit.Type.Name,
                UnitCode = booking.AccommodationUnit.Code,
                ArrivalDate = booking.ArrivalDate,
                DepartureDate = booking.DepartureDate,
                NumberOfGuests = booking.NumberOfGuests,
                Status = status,
            };
    }
}
