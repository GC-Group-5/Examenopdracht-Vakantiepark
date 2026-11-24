using Vakantiepark.Domain.Entities;
using Vakantiepark.Domain.Enums;
using Vakantiepark.Domain.Filters;
using Vakantiepark.Domain.Paging;
using Vakantiepark.Infrastructure.Persistence;
using Vakantiepark.Infrastructure.Repositories;

namespace Vakantiepark.IntegrationTests
{
    // US-13 and US-15: the staff queries on bookings (search, filters and the day overview), against a real PostgreSQL.
    // Every test uses its own unit, type and name, so tests never see each other's bookings.
    public class ReservationSearchTests : IClassFixture<PostgresFixture>
    {
        private static readonly DateOnly Today = new DateOnly(2027, 6, 1);

        private readonly PostgresFixture Database;

        public ReservationSearchTests(PostgresFixture database)
        {
            Database = database;
        }

        [Fact]
        public async Task SearchAsync_SearchText_MatchesBookingNumberNameAndEmailIgnoringCase()
        {
            TestUnit unit = await CreateUnitAsync();
            Booking byName = NewBooking(unit, Today.AddDays(1), Today.AddDays(3), booking => booking.Name = $"Jan Jansen {unit.Token}");
            Booking byEmail = NewBooking(unit, Today.AddDays(5), Today.AddDays(7), booking => booking.Email = $"els.{unit.Token}@example.nl");
            Booking byNumber = NewBooking(unit, Today.AddDays(9), Today.AddDays(11), booking => booking.BookingNumber = $"N{unit.Token}".ToUpperInvariant());
            Booking other = NewBooking(unit, Today.AddDays(13), Today.AddDays(15));
            await SaveAsync(byName, byEmail, byNumber, other);

            PagedResult<Booking> result = await SearchAsync(new BookingSearchFilter() { Search = unit.Token.ToUpperInvariant() });

            Assert.Equal(3, result.TotalCount);
            Assert.DoesNotContain(result.Items, booking => booking.Id == other.Id);
        }

        [Fact]
        public async Task SearchAsync_Status_SplitsConfirmedCompletedAndCancelled()
        {
            TestUnit unit = await CreateUnitAsync();
            Booking confirmed = NewBooking(unit, Today.AddDays(2), Today.AddDays(4));
            Booking completed = NewBooking(unit, Today.AddDays(-6), Today.AddDays(-2));
            Booking cancelled = NewBooking(unit, Today.AddDays(10), Today.AddDays(12), booking => booking.DeletedAt = DateTime.UtcNow);
            await SaveAsync(confirmed, completed, cancelled);

            Assert.Equal(confirmed.Id, Assert.Single((await SearchAsync(unit, BookingState.Confirmed)).Items).Id);
            Assert.Equal(completed.Id, Assert.Single((await SearchAsync(unit, BookingState.Completed)).Items).Id);
            Assert.Equal(cancelled.Id, Assert.Single((await SearchAsync(unit, BookingState.Cancelled)).Items).Id);
            Assert.Equal(3, (await SearchAsync(unit, null)).TotalCount);
        }

        [Fact]
        public async Task SearchAsync_Period_IncludesBookingsThatTouchItOnlyOnTheDepartureDay()
        {
            TestUnit unit = await CreateUnitAsync();
            Booking leavesOnFirstDay = NewBooking(unit, new DateOnly(2027, 7, 3), new DateOnly(2027, 7, 10));
            Booking inside = NewBooking(unit, new DateOnly(2027, 7, 12), new DateOnly(2027, 7, 14));
            Booking afterwards = NewBooking(unit, new DateOnly(2027, 7, 21), new DateOnly(2027, 7, 24));
            await SaveAsync(leavesOnFirstDay, inside, afterwards);

            PagedResult<Booking> result = await SearchAsync(new BookingSearchFilter()
            {
                AccommodationTypeSlug = unit.TypeSlug,
                From = new DateOnly(2027, 7, 10),
                To = new DateOnly(2027, 7, 20),
            });

            Assert.Equal(2, result.TotalCount);
            Assert.DoesNotContain(result.Items, booking => booking.Id == afterwards.Id);
        }

        [Fact]
        public async Task SearchAsync_Paging_ReturnsOnePageAndTheTotalNewestArrivalFirst()
        {
            TestUnit unit = await CreateUnitAsync();
            List<Booking> bookings = Enumerable.Range(0, 5)
                .Select(index => NewBooking(unit, Today.AddDays(index * 3), Today.AddDays(index * 3 + 2)))
                .ToList();
            await SaveAsync(bookings.ToArray());

            PagedResult<Booking> result = await SearchAsync(new BookingSearchFilter() { AccommodationTypeSlug = unit.TypeSlug }, page: 2, pageSize: 2);

            Assert.Equal(5, result.TotalCount);
            Assert.Equal(new List<DateOnly>() { Today.AddDays(6), Today.AddDays(3) }, result.Items.Select(booking => booking.ArrivalDate).ToList());
            Assert.All(result.Items, booking => Assert.Equal(unit.TypeName, booking.AccommodationUnit.Type.Name));
        }

        [Fact]
        public async Task SearchAsync_PercentSign_IsSearchedLiterally()
        {
            TestUnit unit = await CreateUnitAsync();
            await SaveAsync(NewBooking(unit, Today.AddDays(1), Today.AddDays(2), booking => booking.Name = $"Gast {unit.Token}"));

            PagedResult<Booking> result = await SearchAsync(new BookingSearchFilter() { Search = $"{unit.Token}%" });

            Assert.Equal(0, result.TotalCount);
        }

        [Fact]
        public async Task GetArrivingOrDepartingOnAsync_ReturnsArrivalsAndDeparturesOfTheDayOnly()
        {
            TestUnit unit = await CreateUnitAsync();
            Booking departing = NewBooking(unit, Today.AddDays(-4), Today);
            Booking arriving = NewBooking(unit, Today, Today.AddDays(3));
            Booking cancelledArrival = NewBooking(unit, Today, Today.AddDays(2), booking => booking.DeletedAt = DateTime.UtcNow);
            Booking dayBefore = NewBooking(unit, Today.AddDays(-6), Today.AddDays(-4));
            Booking dayAfter = NewBooking(unit, Today.AddDays(3), Today.AddDays(5));
            await SaveAsync(departing, arriving, cancelledArrival, dayBefore, dayAfter);

            await using ApplicationDBContext context = Database.CreateContext();
            List<Booking> bookings = (await new BookingRepository(context).GetArrivingOrDepartingOnAsync(Today))
                .Where(booking => booking.AccommodationUnitId == unit.Id)
                .ToList();

            Assert.Equal(new List<Guid>() { departing.Id, arriving.Id }.Order(), bookings.Select(booking => booking.Id).Order());
            Assert.All(bookings, booking => Assert.Equal($"Z{unit.Token}", booking.AccommodationUnit.Zone.Name));
        }

        private async Task<PagedResult<Booking>> SearchAsync(TestUnit unit, BookingState? state)
            => await SearchAsync(new BookingSearchFilter() { AccommodationTypeSlug = unit.TypeSlug, State = state });

        private async Task<PagedResult<Booking>> SearchAsync(BookingSearchFilter filter, int page = 1, int pageSize = 25)
        {
            await using ApplicationDBContext context = Database.CreateContext();
            return await new BookingRepository(context).SearchAsync(filter, Today, page, pageSize);
        }

        private async Task SaveAsync(params Booking[] bookings)
        {
            await using ApplicationDBContext context = Database.CreateContext();
            context.Bookings.AddRange(bookings);
            await context.SaveChangesAsync();
        }

        private async Task<TestUnit> CreateUnitAsync()
        {
            await using ApplicationDBContext context = Database.CreateContext();
            Guid id = Guid.NewGuid();
            string token = id.ToString("N")[..8];
            AccommodationType type = new AccommodationType { Id = Guid.NewGuid(), Slug = $"t-{token}", Name = $"Type {token}", Category = AccommodationCategory.HolidayHome };
            context.Accommodations.Add(new Accommodation
            {
                Id = id,
                Code = $"T-{token}",
                Name = $"Test {token}",
                MaxPersons = 4,
                Type = type,
                Zone = new Zone { Id = Guid.NewGuid(), Name = $"Z{token}", Category = AccommodationCategory.HolidayHome },
            });
            await context.SaveChangesAsync();
            return new TestUnit(id, token, type.Slug, type.Name);
        }

        private static Booking NewBooking(TestUnit unit, DateOnly arrival, DateOnly departure, Action<Booking>? configure = null)
        {
            Booking booking = new Booking
            {
                Id = Guid.NewGuid(),
                BookingNumber = Guid.NewGuid().ToString("N")[..12],
                AccessToken = Guid.NewGuid().ToString("N"),
                Name = "Test Gast",
                Email = "test@example.com",
                PhoneNumber = "0612345678",
                ArrivalDate = arrival,
                DepartureDate = departure,
                NumberOfGuests = 2,
                AccommodationUnitId = unit.Id,
                CreatedAt = DateTime.UtcNow,
            };
            configure?.Invoke(booking);
            return booking;
        }

        private record TestUnit(Guid Id, string Token, string TypeSlug, string TypeName);
    }
}
