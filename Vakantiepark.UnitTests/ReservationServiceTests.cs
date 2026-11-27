using Moq;
using Vakantiepark.Application.Services;
using Vakantiepark.Domain.Entities;
using Vakantiepark.Domain.Enums;
using Vakantiepark.Domain.Filters;
using Vakantiepark.Domain.Paging;
using Vakantiepark.Domain.Interfaces.Repositories;
using Vakantiepark.Shared.Constants;
using Vakantiepark.Shared.DTOs;
using Vakantiepark.Shared.DTOs.Reservations;
using Xunit;

namespace Vakantiepark.UnitTests
{
    // US-13: the reservations overview and detail page for staff.
    public class ReservationServiceTests
    {
        private static readonly DateOnly Today = new DateOnly(2027, 4, 1);

        private readonly Mock<IBookingRepository> _bookingRepository = new Mock<IBookingRepository>();
        private readonly Mock<ICleaningRepository> _cleaningRepository = new Mock<ICleaningRepository>();
        private readonly ReservationService _sut;
        private BookingSearchFilter? _usedFilter;
        private int _usedPage;
        private int _usedPageSize;

        public ReservationServiceTests()
        {
            _cleaningRepository
                .Setup(x => x.GetByBookingIdsAsync(It.IsAny<List<Guid>>()))
                .ReturnsAsync(new List<Cleaning>());

            _sut = new ReservationService(_bookingRepository.Object, _cleaningRepository.Object, new FixedTimeProvider(new DateTimeOffset(Today.ToDateTime(new TimeOnly(9, 0)), TimeSpan.Zero)));
            ReturnFromSearch([], 0);
        }

        [Fact]
        public async Task Search_PassesPageAndPageSize()
        {
            await _sut.SearchAsync(new ReservationSearchRequest() { Page = 2, PageSize = 10 });

            Assert.Equal(2, _usedPage);
            Assert.Equal(10, _usedPageSize);
        }

        [Fact]
        public async Task Search_PassesSearchTextPeriodTypeAndStatus()
        {
            await _sut.SearchAsync(new ReservationSearchRequest()
            {
                Search = "jansen",
                From = new DateOnly(2027, 7, 1),
                To = new DateOnly(2027, 7, 31),
                AccommodationTypeSlug = "berk-4",
                Status = BookingStatuses.Geannuleerd,
            });

            Assert.NotNull(_usedFilter);
            Assert.Equal("jansen", _usedFilter.Search);
            Assert.Equal(new DateOnly(2027, 7, 1), _usedFilter.From);
            Assert.Equal(new DateOnly(2027, 7, 31), _usedFilter.To);
            Assert.Equal("berk-4", _usedFilter.AccommodationTypeSlug);
            Assert.Equal(BookingState.Cancelled, _usedFilter.State);
        }

        [Fact]
        public async Task Search_NoStatus_FiltersOnNoState()
        {
            await _sut.SearchAsync(new ReservationSearchRequest());

            Assert.Null(_usedFilter?.State);
        }

        [Theory]
        [InlineData(0, 25)]
        [InlineData(1, 0)]
        [InlineData(1, 101)]
        public async Task Search_InvalidPaging_Throws(int page, int pageSize)
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _sut.SearchAsync(new ReservationSearchRequest() { Page = page, PageSize = pageSize }));
        }

        [Fact]
        public async Task Search_PeriodEndsBeforeItStarts_Throws()
        {
            ReservationSearchRequest request = new ReservationSearchRequest() { From = new DateOnly(2027, 7, 10), To = new DateOnly(2027, 7, 1) };

            await Assert.ThrowsAsync<ArgumentException>(() => _sut.SearchAsync(request));
        }

        [Fact]
        public async Task Search_UnknownStatus_Throws()
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _sut.SearchAsync(new ReservationSearchRequest() { Status = "Betaald" }));
        }

        [Fact]
        public async Task Search_ReturnsRowsWithStatusAndPageCount()
        {
            Booking upcoming = CreateBooking(Today.AddDays(5), Today.AddDays(10));
            Booking completed = CreateBooking(Today.AddDays(-10), Today.AddDays(-3));
            Booking cancelled = CreateBooking(Today.AddDays(20), Today.AddDays(25), booking => booking.DeletedAt = DateTime.UtcNow);
            ReturnFromSearch([upcoming, completed, cancelled], 53);

            PagedResponse<ReservationListItemResponse> page = await _sut.SearchAsync(new ReservationSearchRequest() { PageSize = 25 });

            Assert.Equal(new List<string>() { BookingStatuses.Bevestigd, BookingStatuses.Afgerond, BookingStatuses.Geannuleerd }, page.Items.Select(item => item.Status).ToList());
            Assert.Equal(53, page.TotalCount);
            Assert.Equal(3, page.TotalPages);
            Assert.Equal("BRK4-001", page.Items[0].UnitCode);
            Assert.Equal("Berk 4", page.Items[0].AccommodationTypeName);
        }

        [Fact]
        public async Task GetById_Unknown_ThrowsNotFound()
        {
            await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.GetByIdAsync(Guid.NewGuid()));
        }

        [Fact]
        public async Task GetById_ReturnsDetailsPriceAndGuests()
        {
            Booking booking = CreateBooking(Today.AddDays(5), Today.AddDays(10), configure: booking =>
            {
                booking.TotalPrice = 600m;
                booking.CancellationFee = 120m;
                booking.DeletedAt = DateTime.UtcNow;
                booking.Guests =
                [
                    new BookingGuest() { Name = "Els Jansen", BirthDate = new DateOnly(1987, 9, 12) },
                    new BookingGuest() { Name = "Jan Jansen", BirthDate = new DateOnly(1985, 5, 1), IsMainBooker = true },
                ];
            });
            _bookingRepository.Setup(x => x.GetByIdWithDetailsAsync(booking.Id)).ReturnsAsync(booking);

            ReservationDetailResponse detail = await _sut.GetByIdAsync(booking.Id);

            Assert.Equal(booking.Id, detail.Id);
            Assert.Equal("BRK4-001", detail.UnitCode);
            Assert.Equal(600m, detail.TotalPrice);
            Assert.Equal(120m, detail.CancellationFee);
            Assert.Equal(BookingStatuses.Geannuleerd, detail.Status);
            Assert.Equal("Jan Jansen", detail.Guests[0].Name);
        }

        [Fact]
        public async Task GetById_ShowsChangesNewestFirstWithStaffName()
        {
            User staff = TestData.CreateUser();
            Booking booking = CreateBooking(Today.AddDays(5), Today.AddDays(10), configure: booking => booking.Changes =
            [
                new BookingChange() { ChangedAt = new DateTime(2027, 3, 1, 9, 0, 0, DateTimeKind.Utc), Description = "Gewijzigd", User = staff },
                new BookingChange() { ChangedAt = new DateTime(2027, 3, 2, 9, 0, 0, DateTimeKind.Utc), Description = "Geannuleerd", Reason = "Gast ziek", User = staff },
            ]);
            _bookingRepository.Setup(x => x.GetByIdWithDetailsAsync(booking.Id)).ReturnsAsync(booking);

            ReservationDetailResponse detail = await _sut.GetByIdAsync(booking.Id);

            Assert.Equal(new[] { "Geannuleerd", "Gewijzigd" }, detail.Changes.Select(change => change.Description));
            Assert.Equal("Mila Medewerker", detail.Changes[0].StaffName);
            Assert.Equal("Gast ziek", detail.Changes[0].Reason);
        }

        [Fact]
        public async Task GetDayOverview_SplitsArrivalsAndDeparturesOfTheDay()
        {
            Booking arriving = CreateBooking(Today, Today.AddDays(4));
            Booking departing = CreateBooking(Today.AddDays(-3), Today);
            ReturnForDay(Today, arriving, departing);

            DayOverviewResponse overview = await _sut.GetDayOverviewAsync(Today);

            Assert.Equal(Today, overview.Date);
            Assert.Equal(arriving.Id, Assert.Single(overview.Arrivals).Id);
            Assert.Equal(departing.Id, Assert.Single(overview.Departures).Id);
        }

        [Fact]
        public async Task GetDayOverview_StayThatOnlyCoversTheDay_IsInNeitherList()
        {
            Booking staying = CreateBooking(Today.AddDays(-1), Today.AddDays(1));
            ReturnForDay(Today, staying);

            DayOverviewResponse overview = await _sut.GetDayOverviewAsync(Today);

            Assert.Empty(overview.Arrivals);
            Assert.Empty(overview.Departures);
        }

        [Fact]
        public async Task GetDayOverview_SortsByZoneThenUnit()
        {
            Booking bosrandB = CreateBooking(Today, Today.AddDays(2), unit: TestData.CreateAccommodation("B-02", "Bosrand"));
            Booking waterkant = CreateBooking(Today, Today.AddDays(2), unit: TestData.CreateAccommodation("A-01", "Waterkant"));
            Booking bosrandA = CreateBooking(Today, Today.AddDays(2), unit: TestData.CreateAccommodation("B-01", "Bosrand"));
            ReturnForDay(Today, waterkant, bosrandB, bosrandA);

            DayOverviewResponse overview = await _sut.GetDayOverviewAsync(Today);

            Assert.Equal(new List<string>() { "B-01", "B-02", "A-01" }, overview.Arrivals.Select(item => item.UnitCode).ToList());
        }

        [Fact]
        public async Task GetDayOverview_ShowsNameUnitZoneGuestsPetsAndBookingNumber()
        {
            Booking booking = CreateBooking(Today, Today.AddDays(3), booking =>
            {
                booking.NumberOfGuests = 3;
                booking.NumberOfPets = 1;
            });
            ReturnForDay(Today, booking);

            DayOverviewItemResponse arrival = Assert.Single((await _sut.GetDayOverviewAsync(Today)).Arrivals);

            Assert.Equal("Jan Jansen", arrival.Name);
            Assert.Equal("BRK4-001", arrival.UnitCode);
            Assert.Equal("Waterkant", arrival.Zone);
            Assert.Equal("Berk 4", arrival.AccommodationTypeName);
            Assert.Equal(3, arrival.NumberOfGuests);
            Assert.Equal(1, arrival.NumberOfPets);
            Assert.Equal("DOU-7KQ3M9XP", arrival.BookingNumber);
        }

        [Fact]
        public async Task GetDayOverview_Departure_ShowsCleaningStatusAndCleaner()
        {
            Booking departing = CreateBooking(Today.AddDays(-3), Today);
            Booking arriving = CreateBooking(Today, Today.AddDays(4));
            ReturnForDay(Today, departing, arriving);
            _cleaningRepository
                .Setup(x => x.GetByBookingIdsAsync(It.IsAny<List<Guid>>()))
                .ReturnsAsync(new List<Cleaning>() { CreateCleaning(departing, CleaningStatus.Clean) });

            DayOverviewResponse overview = await _sut.GetDayOverviewAsync(Today);

            DayOverviewItemResponse departure = Assert.Single(overview.Departures);
            Assert.Equal(CleaningStatuses.Klaar, departure.CleaningStatus);
            Assert.Equal("Sam Schoonmaker", departure.CleanedBy);
            Assert.Null(Assert.Single(overview.Arrivals).CleaningStatus);
        }

        [Fact]
        public async Task GetDayOverview_DepartureNobodyStarted_IsToClean()
        {
            ReturnForDay(Today, CreateBooking(Today.AddDays(-3), Today));

            DayOverviewItemResponse departure = Assert.Single((await _sut.GetDayOverviewAsync(Today)).Departures);

            Assert.Equal(CleaningStatuses.TeSchoonmaken, departure.CleaningStatus);
            Assert.Null(departure.CleanedBy);
        }

        [Fact]
        public async Task GetById_GuestsStillThere_HasNoCleaningStatus()
        {
            Booking booking = CreateBooking(Today.AddDays(-2), Today.AddDays(3));
            _bookingRepository.Setup(x => x.GetByIdWithDetailsAsync(booking.Id)).ReturnsAsync(booking);

            ReservationDetailResponse detail = await _sut.GetByIdAsync(booking.Id);

            Assert.Null(detail.CleaningStatus);
        }

        [Fact]
        public async Task GetById_Departed_ShowsWhoCleanedItAndWhen()
        {
            Booking booking = CreateBooking(Today.AddDays(-5), Today);
            Cleaning cleaning = CreateCleaning(booking, CleaningStatus.Clean);
            cleaning.FinishedAt = Today.ToDateTime(new TimeOnly(11, 30));
            _bookingRepository.Setup(x => x.GetByIdWithDetailsAsync(booking.Id)).ReturnsAsync(booking);
            _cleaningRepository.Setup(x => x.GetByBookingIdAsync(booking.Id)).ReturnsAsync(cleaning);

            ReservationDetailResponse detail = await _sut.GetByIdAsync(booking.Id);

            Assert.Equal(CleaningStatuses.Klaar, detail.CleaningStatus);
            Assert.Equal("Sam Schoonmaker", detail.CleanedBy);
            Assert.Equal(cleaning.FinishedAt, detail.CleanedAt);
        }

        [Fact]
        public async Task GetById_DepartedNobodyStarted_IsToClean()
        {
            Booking booking = CreateBooking(Today.AddDays(-5), Today.AddDays(-1));
            _bookingRepository.Setup(x => x.GetByIdWithDetailsAsync(booking.Id)).ReturnsAsync(booking);

            ReservationDetailResponse detail = await _sut.GetByIdAsync(booking.Id);

            Assert.Equal(CleaningStatuses.TeSchoonmaken, detail.CleaningStatus);
        }

        [Fact]
        public async Task GetById_Cancelled_HasNoCleaningStatus()
        {
            Booking booking = CreateBooking(Today.AddDays(-5), Today.AddDays(-1), booking => booking.DeletedAt = DateTime.UtcNow);
            _bookingRepository.Setup(x => x.GetByIdWithDetailsAsync(booking.Id)).ReturnsAsync(booking);

            ReservationDetailResponse detail = await _sut.GetByIdAsync(booking.Id);

            Assert.Null(detail.CleaningStatus);
        }

        private static Cleaning CreateCleaning(Booking booking, CleaningStatus status)
            => new Cleaning()
            {
                BookingId = booking.Id,
                Status = status,
                User = TestData.CreateUser(user => user.FullName = "Sam Schoonmaker"),
            };

        private void ReturnForDay(DateOnly date, params Booking[] bookings)
            => _bookingRepository
                .Setup(x => x.GetArrivingOrDepartingOnAsync(date))
                .ReturnsAsync(bookings.ToList());

        private void ReturnFromSearch(List<Booking> bookings, int totalCount)
            => _bookingRepository
                .Setup(x => x.SearchAsync(It.IsAny<BookingSearchFilter>(), Today, It.IsAny<int>(), It.IsAny<int>()))
                .Callback<BookingSearchFilter, DateOnly, int, int>((filter, _, page, pageSize) =>
                {
                    _usedFilter = filter;
                    _usedPage = page;
                    _usedPageSize = pageSize;
                })
                .ReturnsAsync(new PagedResult<Booking>() { Items = bookings, TotalCount = totalCount });

        private static Booking CreateBooking(DateOnly arrival, DateOnly departure, Action<Booking>? configure = null, Accommodation? unit = null)
            => TestData.CreateBooking(Today, booking =>
            {
                booking.ArrivalDate = arrival;
                booking.DepartureDate = departure;
                booking.AccommodationUnit = unit ?? TestData.CreateAccommodation("BRK4-001");
                booking.AccommodationUnit.Type = TestData.CreateAccommodationType(type => type.Name = "Berk 4");
                configure?.Invoke(booking);
            });
    }
}
