using Vakantiepark.Shared.Constants;
using Vakantiepark.Application.Validations.Booking;
using Vakantiepark.Application.Helpers;
using Vakantiepark.Application.Settings;
using Microsoft.Extensions.Logging;
using System.Globalization;
using Vakantiepark.Domain.Interfaces.Repositories;
using Vakantiepark.Domain.Interfaces.Helpers;
using Vakantiepark.Application.Interfaces;
using Vakantiepark.Application.Extensions;
using Vakantiepark.Shared.DTOs.Booking;
using Vakantiepark.Shared.DTOs.Price;
using Vakantiepark.Domain.Entities;
using Vakantiepark.Domain.Filters;

namespace Vakantiepark.Application.Services
{
    public class BookingService : IBookingService
    {
        // A collision is very unlikely with 31^8 possible numbers; this only prevents an endless loop.
        private const int MaxBookingNumberAttempts = 5;

        public const string InvalidLinkMessage = "Deze link is ongeldig.";
        public const string ExpiredLinkMessage = "Deze link is verlopen. Je kunt je boeking nog bekijken door in te loggen met je boekingsnummer en e-mailadres.";
        public const string NotFoundMessage = "We kunnen geen boeking vinden met deze gegevens.";

        private readonly IBookingRepository BookingRepository;
        private readonly IAccommodationUnitRepository AccommodationUnitRepository;
        private readonly IAccommodationTypeRepository AccommodationTypeRepository;
        private readonly IPriceService PriceService;
        private readonly IBookingNumberGenerator BookingNumberGenerator;
        private readonly IRepository<Season> SeasonRepository;
        private readonly IRepository<ParkSettings> ParkSettingsRepository;
        private readonly TimeProvider TimeProvider;
        private readonly IEmailSender EmailSender;
        private readonly FrontendSettings FrontendSettings;
        private readonly ILogger<BookingService> Logger;

        public BookingService(
            IBookingRepository bookingRepository,
            IAccommodationUnitRepository accommodationUnitRepository,
            IAccommodationTypeRepository accommodationTypeRepository,
            IPriceService priceService,
            IBookingNumberGenerator bookingNumberGenerator,
            IRepository<Season> seasonRepository,
            IRepository<ParkSettings> parkSettingsRepository,
            TimeProvider timeProvider,
            IEmailSender emailSender,
            FrontendSettings frontendSettings,
            ILogger<BookingService> logger)
        {
            BookingRepository = bookingRepository;
            AccommodationUnitRepository = accommodationUnitRepository;
            AccommodationTypeRepository = accommodationTypeRepository;
            PriceService = priceService;
            BookingNumberGenerator = bookingNumberGenerator;
            SeasonRepository = seasonRepository;
            ParkSettingsRepository = parkSettingsRepository;
            TimeProvider = timeProvider;
            EmailSender = emailSender;
            FrontendSettings = frontendSettings;
            Logger = logger;
        }

        public async Task<BookingQuoteResponse> GetQuoteAsync(string accommodationTypeSlug, DateOnly arrivalDate, DateOnly departureDate, AccommodationUnitFilter filter, string? preferredZone)
        {
            CreateBookingRequestValidation.ValidateStay(accommodationTypeSlug, arrivalDate, departureDate, filter.NumberOfGuests, filter.NumberOfPets, Today());

            AccommodationType type = await GetTypeAsync(accommodationTypeSlug);
            ParkSettings settings = await GetParkSettingsAsync();
            await ValidateHighSeasonMinimumNightsAsync(arrivalDate, departureDate, settings);
            await GetCandidateUnitsAsync(type, arrivalDate, departureDate, filter, preferredZone);

            PriceSpecificationResponse price = await PriceService.CalculateAsync(type.Slug, arrivalDate, departureDate, filter.NumberOfGuests);

            return new BookingQuoteResponse()
            {
                AccommodationTypeName = type.Name,
                Zone = string.IsNullOrWhiteSpace(preferredZone) ? null : preferredZone.Trim(),
                ArrivalDate = arrivalDate,
                DepartureDate = departureDate,
                ArrivalTime = settings.ArrivalTime,
                DepartureTime = settings.DepartureTime,
                NumberOfGuests = filter.NumberOfGuests,
                Price = price,
                FreeCancellationDaysBeforeArrival = settings.FreeCancellationDaysBeforeArrival,
                NoRefundDaysBeforeArrival = settings.NoRefundDaysBeforeArrival,
                DepositPercentage = settings.DepositPercentage,
            };
        }

        public async Task<BookingResponse> CreateAsync(CreateBookingRequest request)
        {
            request.Validate(Today());

            AccommodationType type = await GetTypeAsync(request.AccommodationTypeSlug);
            ParkSettings settings = await GetParkSettingsAsync();
            await ValidateHighSeasonMinimumNightsAsync(request.ArrivalDate, request.DepartureDate, settings);

            AccommodationUnitFilter filter = new AccommodationUnitFilter()
            {
                NumberOfGuests = request.NumberOfGuests,
                NumberOfPets = request.NumberOfPets,
                WheelchairAccessible = request.WheelchairAccessible,
            };
            List<Accommodation> candidates = await GetCandidateUnitsAsync(type, request.ArrivalDate, request.DepartureDate, filter, request.PreferredZone);

            // The server always calculates the price itself; a price from the browser is never used.
            PriceSpecificationResponse price = await PriceService.CalculateAsync(type.Slug, request.ArrivalDate, request.DepartureDate, request.NumberOfGuests);
            string bookingNumber = await GenerateUniqueBookingNumberAsync();

            // Another guest may book the same unit between the availability check and saving.
            // The database then refuses this booking, and the next free unit is tried.
            foreach (Accommodation unit in candidates)
            {
                Booking booking = request.ToEntity(unit.Id, bookingNumber, price, settings);

                if (await BookingRepository.TryAddAsync(booking))
                {
                    await SendConfirmationAsync(booking, type, settings);
                    return booking.ToResponse();
                }
            }

            throw new ArgumentException(CreateBookingRequestValidation.NotAvailableMessage);
        }

        // The link from the confirmation email works up to and including the departure date.
        public async Task<MyBookingResponse> GetMineByTokenAsync(string token)
        {
            Booking? booking = await BookingRepository.GetByAccessTokenAsync(token);

            if (booking is null)
                throw new ArgumentException(InvalidLinkMessage);

            if (booking.DepartureDate < Today())
                throw new ArgumentException(ExpiredLinkMessage);

            return booking.ToMyBookingResponse(GetStatus(booking));
        }

        // One message for every wrong combination, so it never reveals which of the two is wrong.
        public async Task<MyBookingResponse> GetMineByNumberAndEmailAsync(string bookingNumber, string email)
        {
            Booking? booking = await BookingRepository.GetByBookingNumberAndEmailAsync(bookingNumber, email);

            if (booking is null)
                throw new ArgumentException(NotFoundMessage);

            return booking.ToMyBookingResponse(GetStatus(booking));
        }

        private string GetStatus(Booking booking)
        {
            if (booking.DeletedAt is not null)
                return BookingStatuses.Geannuleerd;

            if (booking.DepartureDate < Today())
                return BookingStatuses.Afgerond;

            return BookingStatuses.Bevestigd;
        }


        // Free units of the type that fit the party (the same filter as the search), in the
        // preferred zone when one is given.
        private async Task<List<Accommodation>> GetCandidateUnitsAsync(AccommodationType type, DateOnly arrivalDate, DateOnly departureDate, AccommodationUnitFilter filter, string? preferredZone)
        {
            List<Accommodation> units = (await AccommodationUnitRepository.GetInServiceUnitsAsync(type.Slug, arrivalDate, departureDate))
                .Where(filter.Matches)
                .ToList();
            if (units.Count == 0)
                throw new ArgumentException(NoFittingUnitMessage(type.Name, filter));

            List<Guid> bookedUnitIds = await BookingRepository.GetUnitIdsWithOverlappingBookingAsync(
                units.Select(unit => unit.Id).ToList(), arrivalDate, departureDate);
            List<Accommodation> freeUnits = units.Where(unit => !bookedUnitIds.Contains(unit.Id)).ToList();
            if (freeUnits.Count == 0)
                throw new ArgumentException(CreateBookingRequestValidation.NotAvailableMessage);

            if (string.IsNullOrWhiteSpace(preferredZone))
                return freeUnits;

            List<Accommodation> inZone = freeUnits
                .Where(unit => string.Equals(unit.Zone.Name, preferredZone.Trim(), StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (inZone.Count == 0)
                throw new ArgumentException($"In de zone {preferredZone.Trim()} is in deze periode geen {type.Name} meer vrij. Kies een andere zone of boek zonder voorkeur.");

            return inZone;
        }

        private static string NoFittingUnitMessage(string typeName, AccommodationUnitFilter filter)
        {
            string place = filter.WheelchairAccessible ? "rolstoeltoegankelijke plek" : "plek";
            string party = $"{filter.NumberOfGuests} personen";
            if (filter.NumberOfPets > 0)
                party += $" met {filter.NumberOfPets} {(filter.NumberOfPets == 1 ? "huisdier" : "huisdieren")}";

            return $"{typeName} heeft geen {place} voor {party}. Kies een ander verblijf.";
        }

        // When the stay falls (partly) in a high season, it must last at least the minimum number of nights.
        private async Task ValidateHighSeasonMinimumNightsAsync(DateOnly arrivalDate, DateOnly departureDate, ParkSettings settings)
        {
            if (settings.HighSeasonMinimumNights < 2)
                return;

            Season? overlappedSeason = (await SeasonRepository.GetAllAsync())
                .Where(season => season.IsHighSeason)
                .FirstOrDefault(season => season.StartDate < departureDate && arrivalDate <= season.EndDate);
            if (overlappedSeason is null)
                return;

            int nights = departureDate.DayNumber - arrivalDate.DayNumber;
            if (nights < settings.HighSeasonMinimumNights)
                throw new ArgumentException($"In het hoogseizoen ({overlappedSeason.Name}) moet een boeking minimaal {settings.HighSeasonMinimumNights} nachten zijn.");
        }

        private async Task<AccommodationType> GetTypeAsync(string accommodationTypeSlug)
            => await AccommodationTypeRepository.GetBySlugAsync(accommodationTypeSlug.Trim().ToLowerInvariant())
                ?? throw new ArgumentException("Dit verblijf bestaat niet. Kies een verblijf uit het overzicht.");

        private async Task<ParkSettings> GetParkSettingsAsync()
            => (await ParkSettingsRepository.GetAllAsync()).FirstOrDefault()
                ?? throw new InvalidOperationException("De parkinstellingen zijn niet ingesteld.");

        private DateOnly Today()
            => DateOnly.FromDateTime(TimeProvider.GetUtcNow().UtcDateTime);

        // US-09: everything is inside the try, so a mail server that is down is logged
        // but never undoes the booking or hides the booking number from the guest.
        private async Task SendConfirmationAsync(Booking booking, AccommodationType type, ParkSettings settings)
        {
            try
            {
                CultureInfo dutch = new CultureInfo("nl-NL");
                string party = string.Join(", ", booking.Guests
                    .OrderByDescending(guest => guest.IsMainBooker)
                    .Select(guest => guest.Name));

                // Personal link to "Mijn boeking" (US-11): the secret token gives access without logging in.
                string myBookingUrl = $"{FrontendSettings.BaseUrl.TrimEnd('/')}/mijn-boeking/{booking.AccessToken}";

                RenderedEmail email = EmailTemplate.Render(EmailTemplate.BookingConfirmation, new Dictionary<string, string>
                {
                    ["Name"] = booking.Name,
                    ["BookingNumber"] = booking.BookingNumber,
                    ["Email"] = booking.Email,
                    ["AccommodationType"] = type.Name,
                    ["ArrivalDate"] = booking.ArrivalDate.ToString("dddd d MMMM yyyy", dutch),
                    ["ArrivalTime"] = booking.ArrivalTime.ToString("HH:mm", dutch),
                    ["DepartureDate"] = booking.DepartureDate.ToString("dddd d MMMM yyyy", dutch),
                    ["DepartureTime"] = booking.DepartureTime.ToString("HH:mm", dutch),
                    ["NumberOfGuests"] = booking.NumberOfGuests == 1 ? "1 persoon" : $"{booking.NumberOfGuests} personen",
                    ["Party"] = party,
                    ["AccommodationPrice"] = booking.AccommodationPrice.ToString("C", dutch),
                    ["CleaningFee"] = booking.CleaningFee.ToString("C", dutch),
                    ["TouristTax"] = booking.TouristTax.ToString("C", dutch),
                    ["TotalPrice"] = booking.TotalPrice.ToString("C", dutch),
                    ["Deposit"] = booking.Deposit.ToString("C", dutch),
                    ["DepositPercentage"] = settings.DepositPercentage.ToString("0.##", dutch),
                    ["FreeCancellationDays"] = settings.FreeCancellationDaysBeforeArrival.ToString(dutch),
                    ["NoRefundDays"] = settings.NoRefundDaysBeforeArrival.ToString(dutch),
                    ["MyBookingUrl"] = myBookingUrl
                });

                await EmailSender.SendAsync(booking.Email, $"Bevestiging van je boeking {booking.BookingNumber}", email.HtmlBody, email.TextBody);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Could not send the confirmation email for booking {BookingNumber}.", booking.BookingNumber);
            }
        }

        private async Task<string> GenerateUniqueBookingNumberAsync()
        {
            for (int attempt = 0; attempt < MaxBookingNumberAttempts; attempt++)
            {
                string bookingNumber = BookingNumberGenerator.Generate();

                if (!await BookingRepository.ExistsByBookingNumberIncludingDeletedAsync(bookingNumber))
                    return bookingNumber;
            }

            throw new InvalidOperationException("Could not generate a unique booking number.");
        }
    }
}
