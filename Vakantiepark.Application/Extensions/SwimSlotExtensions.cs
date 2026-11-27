using Vakantiepark.Domain.Entities;
using Vakantiepark.Shared.DTOs.Swimming;

namespace Vakantiepark.Application.Extensions
{
    public static class SwimSlotExtensions
    {
        // Slots follow each other from opening time; a slot that would run past closing time is left out.
        public static List<SwimSlot> CreateSwimSlots(this ParkSettings settings, DateOnly date)
        {
            if (settings.SwimSlotMinutes <= 0)
                throw new InvalidOperationException("The swim slot duration must be positive.");

            TimeSpan duration = TimeSpan.FromMinutes(settings.SwimSlotMinutes);
            TimeSpan closesAt = settings.PoolClosesAt.ToTimeSpan();
            List<SwimSlot> slots = [];

            for (TimeSpan start = settings.PoolOpensAt.ToTimeSpan(); start + duration <= closesAt; start += duration)
            {
                slots.Add(new SwimSlot
                {
                    Date = date,
                    StartTime = TimeOnly.FromTimeSpan(start),
                    EndTime = TimeOnly.FromTimeSpan(start + duration),
                    Capacity = settings.SwimSlotDefaultCapacity,
                });
            }

            return slots;
        }

        // A guest sees a slot only when it has not started yet and lies between his arrival and departure time.
        public static bool IsAvailableTo(this SwimSlot slot, Booking booking, DateTimeOffset parkNow)
        {
            DateOnly parkToday = DateOnly.FromDateTime(parkNow.DateTime);

            if (slot.Date < booking.ArrivalDate || slot.Date > booking.DepartureDate)
                return false;

            if (slot.Date == booking.ArrivalDate && slot.StartTime < booking.ArrivalTime)
                return false;

            if (slot.Date == booking.DepartureDate && slot.EndTime > booking.DepartureTime)
                return false;

            if (slot.Date < parkToday)
                return false;

            return slot.Date > parkToday || slot.StartTime > TimeOnly.FromDateTime(parkNow.DateTime);
        }

        // No reservations exist before US-23, so every place of a slot is still free.
        public static SwimSlotResponse ToResponse(this SwimSlot slot) => new()
        {
            Id = slot.Id,
            StartTime = slot.StartTime,
            EndTime = slot.EndTime,
            Capacity = slot.Capacity,
            RemainingPlaces = slot.Capacity,
            IsFull = slot.Capacity <= 0,
        };
    }
}
