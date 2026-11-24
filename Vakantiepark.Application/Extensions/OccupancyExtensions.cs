using Vakantiepark.Domain.Entities;
using Vakantiepark.Shared.Constants;
using Vakantiepark.Shared.DTOs.Occupancy;

namespace Vakantiepark.Application.Extensions
{
    public static class OccupancyExtensions
    {
        // MaintenancePeriods must only hold the period that covers the date (periods never overlap).
        public static OccupancyUnitResponse ToOccupancyUnit(this Accommodation unit, Booking? booking)
        {
            MaintenancePeriod? maintenance = unit.MaintenancePeriods.FirstOrDefault();

            return new OccupancyUnitResponse()
            {
                Code = unit.Code,
                TypeName = unit.Type.Name,
                ZoneName = unit.Zone.Name,
                Status = GetStatus(maintenance, booking),
                BookingId = booking?.Id,
                BookingNumber = booking?.BookingNumber,
                GuestName = booking?.Name,
                MaintenanceReason = maintenance?.Reason.ToString(),
                MaintenanceNote = maintenance?.Note,
            };
        }

        // Out of service wins over a booking: the unit can't be used, but staff still see the booking to act on it.
        private static string GetStatus(MaintenancePeriod? maintenance, Booking? booking)
        {
            if (maintenance is not null)
                return OccupancyStatuses.NietVerhuurbaar;

            return booking is null ? OccupancyStatuses.Beschikbaar : OccupancyStatuses.Bezet;
        }
    }
}
