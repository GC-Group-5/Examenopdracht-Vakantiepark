using Vakantiepark.Application.Extensions;
using Vakantiepark.Application.Interfaces;
using Vakantiepark.Application.Validations.MaintenancePeriods;
using Vakantiepark.Domain.Entities;
using Vakantiepark.Domain.Enums;
using Vakantiepark.Domain.Interfaces.Repositories;
using Vakantiepark.Shared.DTOs.Maintenance;

namespace Vakantiepark.Application.Services
{
    // US-17: staff puts a unit out of service for a period. Overlapping bookings are
    // shown and must be confirmed explicitly before the period is registered.
    // The search and booking flows exclude these units through the unit repository.
    public class MaintenancePeriodService : IMaintenancePeriodService
    {
        private readonly IRepository<MaintenancePeriod> PeriodRepository;
        private readonly IAccommodationUnitRepository UnitRepository;
        private readonly IBookingRepository BookingRepository;
        private readonly TimeProvider TimeProvider;

        public MaintenancePeriodService(
            IRepository<MaintenancePeriod> periodRepository,
            IAccommodationUnitRepository unitRepository,
            IBookingRepository bookingRepository,
            TimeProvider timeProvider)
        {
            PeriodRepository = periodRepository;
            UnitRepository = unitRepository;
            BookingRepository = bookingRepository;
            TimeProvider = timeProvider;
        }

        public async Task<MaintenanceUnitOverviewResponse> GetOverviewAsync(string accommodationUnitCode)
        {
            Accommodation unit = await GetUnitAsync(accommodationUnitCode);

            List<MaintenancePeriod> periods = (await GetPeriodsOfUnitAsync(unit.Id))
                .OrderByDescending(period => period.StartDate)
                .ToList();

            MaintenanceUnitOverviewResponse overview = new()
            {
                UnitCode = unit.Code,
                UnitName = unit.Name,
            };

            foreach (MaintenancePeriod period in periods)
            {
                MaintenancePeriodResponse response = ToResponse(period);
                response.ConflictingBookings = await GetConflictsAsync(unit.Id, period.StartDate, period.EndDate);
                overview.Periods.Add(response);
            }

            return overview;
        }

        public async Task<MaintenancePeriodSaveResult> CreateAsync(CreateMaintenancePeriodRequest request)
        {
            Accommodation unit = await GetUnitAsync(request.AccommodationUnitCode);
            MaintenanceReason reason = ParseReason(request.Reason);
            request.StartDate.ValidateMaintenancePeriod(request.EndDate, TimeProvider.GetToday());

            if (reason == MaintenanceReason.Other && string.IsNullOrWhiteSpace(request.Note))
                throw new ArgumentException("Kies bij de reden 'overig' een toelichting.");

            (await GetPeriodsOfUnitAsync(unit.Id)).ValidateNoOverlap(request.StartDate, request.EndDate);

            List<BookingConflictResponse> conflicts = await GetConflictsAsync(unit.Id, request.StartDate, request.EndDate);

            if (conflicts.Count > 0 && !request.ConfirmExistingBookings)
                return RequiresConfirmation(conflicts);

            MaintenancePeriod period = new()
            {
                Id = Guid.NewGuid(),
                AccommodationUnitId = unit.Id,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Reason = reason,
                Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
            };

            await PeriodRepository.AddAsync(period);

            return Saved(period, conflicts);
        }

        // Shortening, ending early or extending (criterion 4). Bookings that already overlapped the
        // old dates were confirmed when the period was registered; only bookings that fall in the
        // added days need a new confirmation.
        public async Task<MaintenancePeriodSaveResult> UpdateAsync(Guid id, UpdateMaintenancePeriodRequest request)
        {
            MaintenancePeriod period = await PeriodRepository.GetByIdAsync(id)
                ?? throw new ArgumentException("De onderhoudsperiode is niet gevonden.");

            request.StartDate.ValidatePeriodChange(request.EndDate, period, TimeProvider.GetToday());
            (await GetPeriodsOfUnitAsync(period.AccommodationUnitId))
                .Where(other => other.Id != period.Id)
                .ValidateNoOverlap(request.StartDate, request.EndDate);

            List<BookingConflictResponse> confirmedBefore = await GetConflictsAsync(period.AccommodationUnitId, period.StartDate, period.EndDate);
            List<BookingConflictResponse> conflicts = await GetConflictsAsync(period.AccommodationUnitId, request.StartDate, request.EndDate);
            List<BookingConflictResponse> newConflicts = conflicts
                .Where(conflict => confirmedBefore.All(confirmed => confirmed.BookingId != conflict.BookingId))
                .ToList();

            if (newConflicts.Count > 0 && !request.ConfirmExistingBookings)
                return RequiresConfirmation(newConflicts);

            period.StartDate = request.StartDate;
            period.EndDate = request.EndDate;
            await PeriodRepository.UpdateAsync(period);

            return Saved(period, conflicts);
        }

        // A running period remains in the history as ended, while a future period is cancelled.
        public async Task EndAsync(Guid id)
        {
            MaintenancePeriod period = await PeriodRepository.GetByIdAsync(id)
                ?? throw new ArgumentException("De onderhoudsperiode is niet gevonden.");

            DateOnly today = TimeProvider.GetToday();
            period.ValidateCanEnd(today);

            if (period.StartDate > today)
            {
                await PeriodRepository.DeleteAsync(period);
                return;
            }

            period.EndDate = today.AddDays(-1);
            if (period.StartDate > period.EndDate)
                period.StartDate = period.EndDate;

            await PeriodRepository.UpdateAsync(period);
        }

        private async Task<List<MaintenancePeriod>> GetPeriodsOfUnitAsync(Guid unitId)
            => (await PeriodRepository.GetAllAsync())
                .Where(period => period.AccommodationUnitId == unitId)
                .ToList();

        private static MaintenancePeriodSaveResult RequiresConfirmation(List<BookingConflictResponse> conflicts) => new()
        {
            Saved = false,
            RequiresConfirmation = true,
            Message = $"{conflicts.Count} boeking(en) vallen in deze periode. Bevestig om de periode vast te leggen; de boekingen worden dan gemarkeerd als 'actie nodig' in het reserveringsoverzicht.",
            ConflictingBookings = conflicts,
        };

        private static MaintenancePeriodSaveResult Saved(MaintenancePeriod period, List<BookingConflictResponse> conflicts) => new()
        {
            Saved = true,
            RequiresConfirmation = false,
            Period = ToResponse(period),
            ConflictingBookings = conflicts,
        };

        private async Task<Accommodation> GetUnitAsync(string accommodationUnitCode)
            => await UnitRepository.GetByCodeWithDetailsAsync(accommodationUnitCode)
                ?? throw new ArgumentException($"Accommodatie '{accommodationUnitCode}' is niet gevonden.");

        private static MaintenanceReason ParseReason(string reason)
            => Enum.TryParse<MaintenanceReason>(reason, out MaintenanceReason parsed) && Enum.IsDefined(parsed)
                ? parsed
                : throw new ArgumentException("Kies onderhoud, technisch probleem of overig als reden.");

        private async Task<List<BookingConflictResponse>> GetConflictsAsync(Guid unitId, DateOnly startDate, DateOnly endDate)
        {
            List<Booking> bookings = await BookingRepository.GetByUnitIdInPeriodAsync(unitId, startDate, endDate);

            return bookings
                .Select(booking => new BookingConflictResponse
                {
                    BookingId = booking.Id,
                    BookingNumber = booking.BookingNumber,
                    Name = booking.Name,
                    ArrivalDate = booking.ArrivalDate,
                    DepartureDate = booking.DepartureDate,
                    NumberOfGuests = booking.NumberOfGuests,
                })
                .ToList();
        }

        private static MaintenancePeriodResponse ToResponse(MaintenancePeriod period) => new()
        {
            Id = period.Id,
            StartDate = period.StartDate,
            EndDate = period.EndDate,
            Reason = period.Reason.ToString(),
            Note = period.Note,
        };
    }
}
