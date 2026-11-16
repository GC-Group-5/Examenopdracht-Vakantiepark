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

        public MaintenancePeriodService(
            IRepository<MaintenancePeriod> periodRepository,
            IAccommodationUnitRepository unitRepository,
            IBookingRepository bookingRepository)
        {
            PeriodRepository = periodRepository;
            UnitRepository = unitRepository;
            BookingRepository = bookingRepository;
        }

        public async Task<MaintenanceUnitOverviewResponse> GetOverviewAsync(string accommodationUnitCode)
        {
            Accommodation unit = await GetUnitAsync(accommodationUnitCode);

            List<MaintenancePeriod> periods = (await PeriodRepository.GetAllAsync())
                .Where(period => period.AccommodationUnitId == unit.Id)
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

        public async Task<CreateMaintenancePeriodResult> CreateAsync(CreateMaintenancePeriodRequest request)
        {
            Accommodation unit = await GetUnitAsync(request.AccommodationUnitCode);
            MaintenanceReason reason = ParseReason(request.Reason);
            request.StartDate.ValidateMaintenancePeriod(request.EndDate);

            if (reason == MaintenanceReason.Other && string.IsNullOrWhiteSpace(request.Note))
                throw new ArgumentException("Kies bij de reden 'overig' een toelichting.");

            List<BookingConflictResponse> conflicts = await GetConflictsAsync(unit.Id, request.StartDate, request.EndDate);

            if (conflicts.Count > 0 && !request.ConfirmExistingBookings)
            {
                return new CreateMaintenancePeriodResult
                {
                    Created = false,
                    RequiresConfirmation = true,
                    Message = $"{conflicts.Count} boeking(en) vallen in deze periode. Bevestig om de periode vast te leggen; de boekingen worden dan gemarkeerd als 'actie nodig' in het reserveringsoverzicht.",
                    ConflictingBookings = conflicts,
                };
            }

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

            return new CreateMaintenancePeriodResult
            {
                Created = true,
                RequiresConfirmation = false,
                Period = ToResponse(period),
                ConflictingBookings = conflicts,
            };
        }

        public async Task<MaintenancePeriodResponse> UpdateAsync(Guid id, UpdateMaintenancePeriodRequest request)
        {
            MaintenancePeriod period = await PeriodRepository.GetByIdAsync(id)
                ?? throw new ArgumentException("De onderhouden periode is niet gevonden.");

            if (request.StartDate < period.StartDate || request.EndDate > period.EndDate)
                throw new ArgumentException("Een periode kan alleen worden ingekort, niet verruimd.");

            request.StartDate.ValidateMaintenancePeriod(request.EndDate);

            if (request.StartDate > period.EndDate)
                throw new ArgumentException("De nieuwe begindatum valt na het einde van de periode.");

            period.StartDate = request.StartDate;
            period.EndDate = request.EndDate;
            await PeriodRepository.UpdateAsync(period);

            return ToResponse(period);
        }

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
