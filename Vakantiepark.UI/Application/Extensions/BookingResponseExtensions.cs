using Vakantiepark.Shared.DTOs.Booking;
using Vakantiepark.Shared.DTOs.Price;

namespace Vakantiepark.UI.Application.Extensions
{
    public static class BookingResponseExtensions
    {
        // A saved booking carries its amounts itself; this gives them the shape <PriceSpecification /> uses.
        public static PriceSpecificationResponse ToPriceSpecification(this BookingResponse booking)
            => new PriceSpecificationResponse()
            {
                AccommodationPrice = booking.AccommodationPrice,
                CleaningFee = booking.CleaningFee,
                PetCleaningFee = booking.PetCleaningFee,
                TouristTax = booking.TouristTax,
                TotalPrice = booking.TotalPrice,
                SecurityDeposit = booking.SecurityDeposit,
                Deposit = booking.Deposit,
            };
    }
}
