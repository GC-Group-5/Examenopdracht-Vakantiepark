using Vakantiepark.Application.Extensions;
using Vakantiepark.Application.Interfaces;
using Vakantiepark.Application.Validations.Swimming;
using Vakantiepark.Domain.Entities;
using Vakantiepark.Domain.Interfaces.Repositories;
using Vakantiepark.Shared.DTOs.Swimming;

namespace Vakantiepark.Application.Services
{
    // US-22: a guest with an active booking sees the swim slots during his stay and how many places are left.
    public class SwimSlotService : ISwimSlotService
    {
        private readonly ISwimSlotRepository SwimSlotRepository;
        private readonly IBookingRepository BookingRepository;
        private readonly IRepository<ParkSettings> ParkSettingsRepository;
        private readonly TimeProvider TimeProvider;

        public SwimSlotService(
            ISwimSlotRepository swimSlotRepository,
            IBookingRepository bookingRepository,
            IRepository<ParkSettings> parkSettingsRepository,
            TimeProvider timeProvider)
        {
            SwimSlotRepository = swimSlotRepository;
            BookingRepository = bookingRepository;
            ParkSettingsRepository = parkSettingsRepository;
            TimeProvider = timeProvider;
        }

        // Days of the stay that still have a slot to choose; worked out from the settings, so nothing is stored yet.
        public async Task<List<DateOnly>> GetDaysAsync(Guid bookingId)
        {
            Booking booking = await GetBookingAsync(bookingId);
            ParkSettings settings = await GetSettingsAsync();
            DateTimeOffset parkNow = TimeProvider.GetParkNow();

            List<DateOnly> days = [];
            for (DateOnly date = booking.ArrivalDate; date <= booking.DepartureDate; date = date.AddDays(1))
            {
                if (settings.CreateSwimSlots(date).Any(slot => slot.IsAvailableTo(booking, parkNow)))
                    days.Add(date);
            }

            return days;
        }

        public async Task<List<SwimSlotResponse>> GetByDateAsync(Guid bookingId, DateOnly date)
        {
            Booking booking = await GetBookingAsync(bookingId);
            DateTimeOffset parkNow = TimeProvider.GetParkNow();
            date.ValidateSwimDate(booking.ArrivalDate, booking.DepartureDate, DateOnly.FromDateTime(parkNow.DateTime));

            List<SwimSlot> slots = await GetOrCreateSlotsAsync(date);

            return slots
                .Where(slot => slot.IsAvailableTo(booking, parkNow))
                .Select(slot => slot.ToResponse())
                .ToList();
        }

        private async Task<List<SwimSlot>> GetOrCreateSlotsAsync(DateOnly date)
        {
            List<SwimSlot> slots = await SwimSlotRepository.GetByDateAsync(date);
            if (slots.Count > 0)
                return slots;

            List<SwimSlot> newSlots = (await GetSettingsAsync()).CreateSwimSlots(date);
            if (await SwimSlotRepository.TryAddRangeAsync(newSlots))
                return newSlots;

            return await SwimSlotRepository.GetByDateAsync(date);
        }

        // A cancelled booking is soft-deleted and therefore not found: only active bookings may swim.
        private async Task<Booking> GetBookingAsync(Guid bookingId)
            => await BookingRepository.GetByIdAsync(bookingId)
                ?? throw new KeyNotFoundException("Je boeking is niet gevonden of geannuleerd.");

        private async Task<ParkSettings> GetSettingsAsync()
            => (await ParkSettingsRepository.GetAllAsync()).FirstOrDefault()
                ?? throw new InvalidOperationException("De parkinstellingen zijn niet ingesteld.");
    }
}
