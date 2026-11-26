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
using Vakantiepark.Shared.DTOs.Reservations;
using Vakantiepark.Domain.Entities;
using Vakantiepark.Domain.Filters;
using Vakantiepark.Domain.Constants;

namespace Vakantiepark.Application.Services
{
    public class BookingService : IBookingService
    {
        // A collision is very unlikely with 31^8 possible numbers; this only prevents an endless loop.
        private const int MaxBookingNumberAttempts = 5;

        public const string InvalidLinkMessage = "Deze link is ongeldig.";
        public const string ExpiredLinkMessage = "Deze link is verlopen, omdat je verblijf voorbij is.";
        public const string NotFoundMessage = "We kunnen geen boeking vinden met deze gegevens.";
        public const string CannotChangeMessage = "Deze boeking kun je niet meer zelf wijzigen. Neem contact op met de receptie, dan helpen we je verder.";
        public const string NoUnitForChangeMessage = "Er is in de nieuwe periode geen vrij verblijf van dit type. Je boeking is niet gewijzigd.";
        public const string CannotCancelMessage = "Deze boeking kan niet meer geannuleerd worden. Annuleren kan tot de dag van aankomst.";
        public const string AlreadyCancelledMessage = "Deze boeking is al geannuleerd.";
        public const string ChosenUnitNotFreeMessage = "De gekozen eenheid is in deze periode niet vrij of past niet bij het gezelschap.";
        public const string ReservationNotFoundMessage = "Deze reservering bestaat niet.";
        public const string ReasonRequiredMessage = "Vul een reden voor de annulering in.";
        public const string StayEndedMessage = "Een afgelopen verblijf kan niet meer geannuleerd worden.";

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

        public async Task<BookingQuoteResponse> GetQuoteAsync(string accommodationTypeSlug, DateOnly arrivalDate, DateOnly departureDate, AccommodationUnitFilter filter, int numberOfChildrenUnder12, string? preferredZone)
        {
            CreateBookingRequestValidation.ValidateStay(accommodationTypeSlug, arrivalDate, departureDate, filter.NumberOfGuests, filter.NumberOfPets, Today());

            AccommodationType type = await GetTypeAsync(accommodationTypeSlug);
            ParkSettings settings = await GetParkSettingsAsync();
            await ValidateHighSeasonMinimumNightsAsync(arrivalDate, departureDate, settings);
            int maxPets = await GetMaximumPetsAsync(type.Slug, arrivalDate, departureDate);
            CreateBookingRequestValidation.ValidatePetCount(filter.NumberOfPets, maxPets);
            await GetCandidateUnitsAsync(type, arrivalDate, departureDate, filter, preferredZone);

            PriceSpecificationResponse price = await PriceService.CalculateAsync(type.Slug, arrivalDate, departureDate, filter.NumberOfGuests, numberOfChildrenUnder12, filter.NumberOfPets);

            return new BookingQuoteResponse()
            {
                AccommodationTypeName = type.Name,
                Zone = string.IsNullOrWhiteSpace(preferredZone) ? null : preferredZone.Trim(),
                ArrivalDate = arrivalDate,
                DepartureDate = departureDate,
                ArrivalTime = settings.ArrivalTime,
                DepartureTime = settings.DepartureTime,
                NumberOfGuests = filter.NumberOfGuests,
                NumberOfChildrenUnder12 = numberOfChildrenUnder12,
                MaxPets = maxPets,
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
            int maxPets = await GetMaximumPetsAsync(type.Slug, request.ArrivalDate, request.DepartureDate);
            CreateBookingRequestValidation.ValidatePetCount(request.NumberOfPets, maxPets);

            AccommodationUnitFilter filter = new AccommodationUnitFilter()
            {
                NumberOfGuests = request.NumberOfGuests,
                NumberOfPets = request.NumberOfPets,
                WheelchairAccessible = request.WheelchairAccessible,
            };
            List<Accommodation> candidates = await GetCandidateUnitsAsync(type, request.ArrivalDate, request.DepartureDate, filter, request.PreferredZone);

            // The server always calculates the price itself; a price from the browser is never used.
            PriceSpecificationResponse price = await PriceService.CalculateAsync(type.Slug, request.ArrivalDate, request.DepartureDate, request.NumberOfGuests, request.NumberOfChildrenUnder12, request.NumberOfPets);
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

            return ToMine(booking, await GetParkSettingsAsync());
        }

        // For a guest logged in via /Auth/Guest; the id comes from his token, never from the request.
        public async Task<MyBookingResponse> GetMineAsync(Guid bookingId)
        {
            Booking? booking = await BookingRepository.GetByIdWithDetailsAsync(bookingId);

            if (booking is null)
                throw new ArgumentException(NotFoundMessage);

            return ToMine(booking, await GetParkSettingsAsync());
        }

        public async Task<CancellationQuoteResponse> GetCancellationQuoteAsync(BookingAccessRequest access, Guid? guestBookingId)
        {
            Booking booking = await FindOwnBookingAsync(access.Token, guestBookingId);
            ParkSettings settings = await GetParkSettingsAsync();

            if (!CancellationPolicy.CanCancel(booking, Today()))
                throw new ArgumentException(CannotCancelMessage);

            return CancellationQuote(booking, settings, forStaff: false);
        }

        // The same costs for a guest and staff (US-12, US-14); only the explanation is worded for who reads it.
        private CancellationQuoteResponse CancellationQuote(Booking booking, ParkSettings settings, bool forStaff)
        {
            decimal fee = CancellationPolicy.CalculateFee(booking, settings, Today());

            return new CancellationQuoteResponse()
            {
                DaysBeforeArrival = CancellationPolicy.DaysBeforeArrival(booking.ArrivalDate, Today()),
                TotalPrice = booking.TotalPrice,
                CancellationFee = fee,
                Explanation = ExplainFee(booking, settings, fee, forStaff),
            };
        }

        // The fee is calculated again here; an amount from the browser is never used.
        public async Task<MyBookingResponse> CancelAsync(BookingAccessRequest access, Guid? guestBookingId)
        {
            Booking booking = await FindOwnBookingAsync(access.Token, guestBookingId);
            ParkSettings settings = await GetParkSettingsAsync();

            if (!CancellationPolicy.CanCancel(booking, Today()))
                throw new ArgumentException(CannotCancelMessage);

            decimal fee = CancellationPolicy.CalculateFee(booking, settings, Today());
            await BookingRepository.CancelAsync(booking, fee);
            await SendCancellationAsync(booking, settings, fee);

            return ToMine(booking, settings);
        }

        // Checks the change and returns the old and the new price; nothing is saved yet.
        public async Task<ChangeQuoteResponse> GetChangeQuoteAsync(ChangeBookingRequest request, Guid? guestBookingId)
        {
            ChangePlan plan = await PrepareChangeAsync(request, guestBookingId);
            PriceSpecificationResponse newPrice = await PriceService.CalculateAsync(plan.Type.Slug, request.ArrivalDate, request.DepartureDate, request.NumberOfGuests, request.NumberOfChildrenUnder12, plan.Booking.NumberOfPets);

            return new ChangeQuoteResponse()
            {
                OldTotalPrice = plan.Booking.TotalPrice,
                NewPrice = newPrice,
                OtherUnit = plan.OtherUnit,
            };
        }

        // Every check is done again, so the change is safe even when the quote is old.
        public async Task<MyBookingResponse> ChangeAsync(ChangeBookingRequest request, Guid? guestBookingId)
        {
            ChangePlan plan = await PrepareChangeAsync(request, guestBookingId);
            Booking saved = await ApplyChangeAsync(plan, request);
            return ToMine(saved, plan.Settings);
        }

        // Prices the new stay, saves it and tells the guest; shared by the guest and staff.
        private async Task<Booking> ApplyChangeAsync(ChangePlan plan, ChangeBookingRequest request)
        {
            Booking booking = plan.Booking;
            PriceSpecificationResponse newPrice = await PriceService.CalculateAsync(plan.Type.Slug, request.ArrivalDate, request.DepartureDate, request.NumberOfGuests, request.NumberOfChildrenUnder12, booking.NumberOfPets);
            decimal oldTotalPrice = booking.TotalPrice;

            booking.ArrivalDate = request.ArrivalDate;
            booking.DepartureDate = request.DepartureDate;
            booking.NumberOfGuests = request.NumberOfGuests;
            booking.NumberOfChildrenUnder12 = request.NumberOfChildrenUnder12;
            // Only the key: the unit itself was loaded without tracking and must not be saved again.
            booking.AccommodationUnitId = plan.Unit.Id;
            booking.AccommodationPrice = newPrice.AccommodationPrice;
            booking.CleaningFee = newPrice.CleaningFee;
            booking.PetCleaningFee = newPrice.PetCleaningFee;
            booking.TouristTax = newPrice.TouristTax;
            booking.TotalPrice = newPrice.TotalPrice;
            booking.Deposit = newPrice.Deposit;

            // Someone else may have booked the unit since the check; then nothing is saved.
            if (!await BookingRepository.TryUpdateAsync(booking, request.CoTravellers.ToGuests()))
                throw new ArgumentException(NoUnitForChangeMessage);

            await SendChangeAsync(booking, plan.Type, oldTotalPrice);

            // Loaded again, so a new unit (and its zone) is on the booking that is returned.
            return await BookingRepository.GetByIdWithDetailsAsync(booking.Id) ?? booking;
        }

        // US-14: the same rules as a guest (availability, price), but without the term. Staff may
        // also edit the main booker and pick the unit; every change is logged with who and when.
        public async Task ChangeByStaffAsync(Guid bookingId, StaffChangeBookingRequest request, Guid staffUserId)
        {
            Booking booking = await FindReservationAsync(bookingId);

            // First the new details, so the age check below uses the new birth date.
            UpdateMainBooker(booking, request);

            ParkSettings settings = await GetParkSettingsAsync();
            ChangePlan plan = await PlanChangeAsync(booking, settings, request, request.AccommodationUnitCode);

            // No Id: EF then sees a new row; a set Id on a tracked booking makes it try an UPDATE.
            booking.Changes.Add(new BookingChange()
            {
                ChangedAt = DateTime.UtcNow,
                Description = $"Gewijzigd: {request.ArrivalDate:dd-MM-yyyy} t/m {request.DepartureDate:dd-MM-yyyy}, {request.NumberOfGuests} personen, eenheid {plan.Unit.Code}",
                UserId = staffUserId,
            });

            await ApplyChangeAsync(plan, request);
        }

        public async Task<CancellationQuoteResponse> GetCancellationQuoteForStaffAsync(Guid bookingId)
        {
            Booking booking = await FindCancellableByStaffAsync(bookingId);
            ParkSettings settings = await GetParkSettingsAsync();

            return CancellationQuote(booking, settings, forStaff: true);
        }

        // Unlike a guest, staff may also cancel on or after the arrival day; the fee follows US-12.
        public async Task CancelByStaffAsync(Guid bookingId, StaffCancelBookingRequest request, Guid staffUserId)
        {
            if (string.IsNullOrWhiteSpace(request.Reason))
                throw new ArgumentException(ReasonRequiredMessage);

            Booking booking = await FindCancellableByStaffAsync(bookingId);
            ParkSettings settings = await GetParkSettingsAsync();
            decimal fee = CancellationPolicy.CalculateFee(booking, settings, Today());

            // No Id: EF then sees a new row; a set Id on a tracked booking makes it try an UPDATE.
            booking.Changes.Add(new BookingChange()
            {
                ChangedAt = DateTime.UtcNow,
                Description = "Geannuleerd",
                Reason = request.Reason.Trim(),
                UserId = staffUserId,
            });

            await BookingRepository.CancelAsync(booking, fee);
            await SendCancellationAsync(booking, settings, fee);
        }

        private static void UpdateMainBooker(Booking booking, StaffChangeBookingRequest request)
        {
            booking.Name = request.Name.Trim();
            booking.Email = request.Email.Trim();
            booking.PhoneNumber = request.PhoneNumber.Trim();

            BookingGuest? mainBooker = booking.Guests.FirstOrDefault(guest => guest.IsMainBooker);
            if (mainBooker is null)
                return;

            mainBooker.Name = booking.Name;
            mainBooker.BirthDate = request.BirthDate;
        }

        // Staff may open any booking, but never change one that is already cancelled.
        private async Task<Booking> FindReservationAsync(Guid bookingId)
        {
            Booking booking = await BookingRepository.GetByIdWithDetailsAsync(bookingId)
                ?? throw new KeyNotFoundException(ReservationNotFoundMessage);

            if (booking.DeletedAt is not null)
                throw new ArgumentException(AlreadyCancelledMessage);

            return booking;
        }

        // Staff may cancel on or after the arrival day, but not once the guest has left.
        private async Task<Booking> FindCancellableByStaffAsync(Guid bookingId)
        {
            Booking booking = await FindReservationAsync(bookingId);

            if (booking.DepartureDate <= Today())
                throw new ArgumentException(StayEndedMessage);

            return booking;
        }

        private sealed record ChangePlan(Booking Booking, ParkSettings Settings, AccommodationType Type, Accommodation Unit, bool OtherUnit);

        // The guest's change (US-12): own booking and in time; the other rules are in PlanChangeAsync.
        private async Task<ChangePlan> PrepareChangeAsync(ChangeBookingRequest request, Guid? guestBookingId)
        {
            Booking booking = await FindOwnBookingAsync(request.Token, guestBookingId);
            ParkSettings settings = await GetParkSettingsAsync();

            if (!CancellationPolicy.CanChange(booking, settings, Today()))
                throw new ArgumentException(CannotChangeMessage);

            return await PlanChangeAsync(booking, settings, request, chosenUnitCode: null);
        }

        // The rules a guest and staff share: valid stay and party, high season, and a free unit of
        // the same type (the chosen one, else the current one when it is still free, else another).
        private async Task<ChangePlan> PlanChangeAsync(Booking booking, ParkSettings settings, ChangeBookingRequest request, string? chosenUnitCode)
        {
            AccommodationType type = booking.AccommodationUnit.Type;

            CreateBookingRequestValidation.ValidateStay(type.Slug, request.ArrivalDate, request.DepartureDate, request.NumberOfGuests, booking.NumberOfPets, Today());

            BookingGuest? mainBooker = booking.Guests.FirstOrDefault(guest => guest.IsMainBooker);
            if (mainBooker?.BirthDate is DateOnly mainBirthDate && CreateBookingRequestValidation.AgeOn(mainBirthDate, request.ArrivalDate) < CreateBookingRequestValidation.MinimumMainBookerAge)
                throw new ArgumentException($"De hoofdboeker moet op de aankomstdatum minimaal {CreateBookingRequestValidation.MinimumMainBookerAge} jaar zijn.");

            CreateBookingRequestValidation.ValidateChildrenUnder12(request.NumberOfGuests, request.NumberOfChildrenUnder12);
            CreateBookingRequestValidation.ValidateCoTravellers(request.NumberOfGuests, request.CoTravellers, Today());
            await ValidateHighSeasonMinimumNightsAsync(request.ArrivalDate, request.DepartureDate, settings);

            AccommodationUnitFilter filter = new AccommodationUnitFilter()
            {
                NumberOfGuests = request.NumberOfGuests,
                NumberOfPets = booking.NumberOfPets,
                WheelchairAccessible = booking.WheelchairAccessible,
            };
            List<Accommodation> units = (await AccommodationUnitRepository.GetInServiceUnitsAsync(type.Slug, request.ArrivalDate, request.DepartureDate))
                .Where(filter.Matches)
                .ToList();
            List<Guid> bookedUnitIds = await BookingRepository.GetUnitIdsWithOverlappingBookingExcludingAsync(
                units.Select(unit => unit.Id).ToList(), request.ArrivalDate, request.DepartureDate, booking.Id);
            List<Accommodation> freeUnits = units.Where(unit => !bookedUnitIds.Contains(unit.Id)).ToList();

            if (!string.IsNullOrWhiteSpace(chosenUnitCode))
            {
                Accommodation chosenUnit = freeUnits.FirstOrDefault(unit => unit.Code.Equals(chosenUnitCode.Trim(), StringComparison.OrdinalIgnoreCase))
                    ?? throw new ArgumentException(ChosenUnitNotFreeMessage);

                return new ChangePlan(booking, settings, type, chosenUnit, OtherUnit: chosenUnit.Id != booking.AccommodationUnitId);
            }

            Accommodation? currentUnit = freeUnits.FirstOrDefault(unit => unit.Id == booking.AccommodationUnitId);
            if (currentUnit is not null)
                return new ChangePlan(booking, settings, type, currentUnit, OtherUnit: false);

            Accommodation otherUnit = freeUnits.FirstOrDefault()
                ?? throw new ArgumentException(NoUnitForChangeMessage);

            return new ChangePlan(booking, settings, type, otherUnit, OtherUnit: true);
        }

        // The same two ways in as "Mijn boeking": the code from the email link, or the booking id
        // from the guest login token (/Auth/Guest). Never an id from the request itself.
        private async Task<Booking> FindOwnBookingAsync(string? token, Guid? guestBookingId)
        {
            Booking booking;
            if (!string.IsNullOrWhiteSpace(token))
                booking = await BookingRepository.GetByAccessTokenAsync(token)
                    ?? throw new ArgumentException(InvalidLinkMessage);
            else if (guestBookingId is Guid bookingId)
                booking = await BookingRepository.GetByIdWithDetailsAsync(bookingId)
                    ?? throw new ArgumentException(NotFoundMessage);
            else
                throw new ArgumentException(NotFoundMessage);

            // Another tab or the reception may have cancelled it since the page was opened.
            if (booking.DeletedAt is not null)
                throw new ArgumentException(AlreadyCancelledMessage);

            return booking;
        }

        private static string ExplainFee(Booking booking, ParkSettings settings, decimal fee, bool forStaff)
        {
            CultureInfo dutch = new CultureInfo("nl-NL");
            int freeCancellationDays = CancellationPolicy.FreeCancellationDays(booking, settings);
            int noRefundDays = CancellationPolicy.NoRefundDays(booking, settings);

            if (fee == 0m && forStaff)
                return $"Je annuleert een boeking van meer dan {freeCancellationDays} dagen voor aankomst. De boeker krijgt alles terug, inclusief de aanbetaling.";

            if (fee == 0m)
                return $"Je annuleert meer dan {freeCancellationDays} dagen voor aankomst. Dat is kosteloos: je krijgt alles terug, ook de aanbetaling.";

            if (fee == booking.Deposit && forStaff)
                return $"Je annuleert een boeking tussen de {noRefundDays} en {freeCancellationDays} dagen voor aankomst. De boeker krijgt de rest terug, de aanbetaling ({fee.ToString("C", dutch)}) vervalt.";

            if (fee == booking.Deposit)
                return $"Je annuleert {noRefundDays} tot {freeCancellationDays} dagen voor aankomst. De aanbetaling ({fee.ToString("C", dutch)}) vervalt; de rest krijg je terug.";

            if (forStaff)
                return $"Je annuleert een boeking van minder dan {noRefundDays} dagen voor aankomst. De boeker betaalt het volledige verblijf ({fee.ToString("C", dutch)}); de toeristenbelasting ({booking.TouristTax.ToString("C", dutch)}) wordt terugbetaald.";

            return $"Je annuleert minder dan {noRefundDays} dagen voor aankomst. Je betaalt het volledige verblijf ({fee.ToString("C", dutch)}); de toeristenbelasting ({booking.TouristTax.ToString("C", dutch)}) krijg je terug.";
        }

        // "Mijn boeking" plus what the page may offer (US-12) and the costs of a cancelled booking.
        private MyBookingResponse ToMine(Booking booking, ParkSettings settings)
        {
            MyBookingResponse response = booking.ToMyBookingResponse(GetStatus(booking));
            response.CanCancel = CancellationPolicy.CanCancel(booking, Today());
            response.CanChange = CancellationPolicy.CanChange(booking, settings, Today());
            response.CancellationFee = booking.CancellationFee;
            return response;
        }

        private string GetStatus(Booking booking)
            => BookingStatus.Get(booking, Today());


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

        private async Task<int> GetMaximumPetsAsync(string accommodationTypeSlug, DateOnly arrivalDate, DateOnly departureDate)
        {
            List<Accommodation> units = await AccommodationUnitRepository.GetInServiceUnitsAsync(accommodationTypeSlug, arrivalDate, departureDate);
            decimal maximum = units
                .Select(unit => unit.GetNumber(FeatureKeys.MaxPets) ?? 0m)
                .DefaultIfEmpty(0m)
                .Max();

            return (int)maximum;
        }

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
                    ["Pets"] = booking.NumberOfPets switch { 0 => "geen", 1 => "1 huisdier", _ => $"{booking.NumberOfPets} huisdieren" },
                    ["WheelchairAccessible"] = booking.WheelchairAccessible ? "ja" : "nee",
                    ["AccommodationPrice"] = booking.AccommodationPrice.ToString("C", dutch),
                    ["CleaningFee"] = booking.CleaningFee.ToString("C", dutch),
                    ["PetCleaningFee"] = booking.PetCleaningFee.ToString("C", dutch),
                    ["TouristTax"] = booking.TouristTax.ToString("C", dutch),
                    ["TotalPrice"] = booking.TotalPrice.ToString("C", dutch),
                    ["SecurityDeposit"] = booking.SecurityDeposit.ToString("C", dutch),
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

        // Like the confirmation: a mail server that is down is logged, the cancellation stays.
        private async Task SendCancellationAsync(Booking booking, ParkSettings settings, decimal fee)
        {
            try
            {
                CultureInfo dutch = new CultureInfo("nl-NL");

                RenderedEmail email = EmailTemplate.Render(EmailTemplate.BookingCancellation, new Dictionary<string, string>
                {
                    ["Name"] = booking.Name,
                    ["BookingNumber"] = booking.BookingNumber,
                    ["AccommodationType"] = booking.AccommodationUnit.Type.Name,
                    ["ArrivalDate"] = booking.ArrivalDate.ToString("dddd d MMMM yyyy", dutch),
                    ["DepartureDate"] = booking.DepartureDate.ToString("dddd d MMMM yyyy", dutch),
                    ["CancellationFee"] = fee.ToString("C", dutch),
                    ["FeeExplanation"] = ExplainFee(booking, settings, fee, forStaff: false),
                    ["MyBookingUrl"] = $"{FrontendSettings.BaseUrl.TrimEnd('/')}/mijn-boeking/{booking.AccessToken}"
                });

                await EmailSender.SendAsync(booking.Email, $"Annulering van je boeking {booking.BookingNumber}", email.HtmlBody, email.TextBody);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Could not send the cancellation email for booking {BookingNumber}.", booking.BookingNumber);
            }
        }

        private async Task SendChangeAsync(Booking booking, AccommodationType type, decimal oldTotalPrice)
        {
            try
            {
                CultureInfo dutch = new CultureInfo("nl-NL");
                string party = string.Join(", ", booking.Guests
                    .OrderByDescending(guest => guest.IsMainBooker)
                    .Select(guest => guest.Name));

                RenderedEmail email = EmailTemplate.Render(EmailTemplate.BookingChange, new Dictionary<string, string>
                {
                    ["Name"] = booking.Name,
                    ["BookingNumber"] = booking.BookingNumber,
                    ["AccommodationType"] = type.Name,
                    ["ArrivalDate"] = booking.ArrivalDate.ToString("dddd d MMMM yyyy", dutch),
                    ["ArrivalTime"] = booking.ArrivalTime.ToString("HH:mm", dutch),
                    ["DepartureDate"] = booking.DepartureDate.ToString("dddd d MMMM yyyy", dutch),
                    ["DepartureTime"] = booking.DepartureTime.ToString("HH:mm", dutch),
                    ["NumberOfGuests"] = booking.NumberOfGuests == 1 ? "1 persoon" : $"{booking.NumberOfGuests} personen",
                    ["Party"] = party,
                    ["OldTotalPrice"] = oldTotalPrice.ToString("C", dutch),
                    ["NewTotalPrice"] = booking.TotalPrice.ToString("C", dutch),
                    ["SecurityDeposit"] = booking.SecurityDeposit.ToString("C", dutch),
                    ["Deposit"] = booking.Deposit.ToString("C", dutch),
                    ["MyBookingUrl"] = $"{FrontendSettings.BaseUrl.TrimEnd('/')}/mijn-boeking/{booking.AccessToken}"
                });

                await EmailSender.SendAsync(booking.Email, $"Wijziging van je boeking {booking.BookingNumber}", email.HtmlBody, email.TextBody);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Could not send the change email for booking {BookingNumber}.", booking.BookingNumber);
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
