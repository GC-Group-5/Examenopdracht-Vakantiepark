using Vakantiepark.Application.Extensions;
using Vakantiepark.Application.Helpers;
using Vakantiepark.Application.Interfaces;
using Vakantiepark.Application.Validations.Reservations;
using Vakantiepark.Domain.Entities;
using Vakantiepark.Domain.Enums;
using Vakantiepark.Domain.Filters;
using Vakantiepark.Domain.Paging;
using Vakantiepark.Domain.Interfaces.Repositories;
using Vakantiepark.Shared.DTOs;
using Vakantiepark.Shared.DTOs.Reservations;

namespace Vakantiepark.Application.Services
{
    public class ReservationService : IReservationService
    {
        public const string NotFoundMessage = "Deze reservering bestaat niet.";

        private readonly IBookingRepository BookingRepository;
        private readonly ICleaningRepository CleaningRepository;
        private readonly TimeProvider TimeProvider;

        public ReservationService(IBookingRepository bookingRepository, ICleaningRepository cleaningRepository, TimeProvider timeProvider)
        {
            BookingRepository = bookingRepository;
            CleaningRepository = cleaningRepository;
            TimeProvider = timeProvider;
        }

        public async Task<PagedResponse<ReservationListItemResponse>> SearchAsync(ReservationSearchRequest request)
        {
            request.Validate();

            BookingSearchFilter filter = new BookingSearchFilter()
            {
                Search = request.Search,
                From = request.From,
                To = request.To,
                State = ReservationSearchValidation.ParseStatus(request.Status),
                AccommodationTypeSlug = request.AccommodationTypeSlug,
            };

            DateOnly today = TimeProvider.GetToday();
            PagedResult<Booking> result = await BookingRepository.SearchAsync(filter, today, request.Page, request.PageSize);

            return result.ToPagedResponse(request, booking => booking.ToReservationListItem(BookingStatus.Get(booking, today)));
        }

        public async Task<ReservationDetailResponse> GetByIdAsync(Guid id)
        {
            Booking booking = await BookingRepository.GetByIdWithDetailsAsync(id)
                ?? throw new KeyNotFoundException(NotFoundMessage);

            DateOnly today = TimeProvider.GetToday();
            ReservationDetailResponse response = booking.ToMyBookingResponse<ReservationDetailResponse>(BookingStatus.Get(booking, today));
            response.Id = booking.Id;
            response.UnitCode = booking.AccommodationUnit.Code;
            response.CreatedAt = booking.CreatedAt;
            response.CancellationFee = booking.CancellationFee;
            response.Changes = booking.Changes
                .OrderByDescending(change => change.ChangedAt)
                .Select(change => change.ToResponse())
                .ToList();

            // US-32: a cancelled booking or one whose guests are still there has nothing to clean yet.
            Cleaning? cleaning = await CleaningRepository.GetByBookingIdAsync(booking.Id);
            if (cleaning is not null || (booking.DeletedAt is null && booking.DepartureDate <= today))
            {
                response.CleaningStatus = (cleaning?.Status ?? CleaningStatus.ToClean).ToText();
                response.CleanedBy = cleaning?.User?.FullName;
                response.CleanedAt = cleaning?.FinishedAt;
            }

            return response;
        }

        public async Task<DayOverviewResponse> GetDayOverviewAsync(DateOnly date)
        {
            List<Booking> bookings = await BookingRepository.GetArrivingOrDepartingOnAsync(date);
            List<Guid> departureIds = bookings
                .Where(booking => booking.DepartureDate == date)
                .Select(booking => booking.Id)
                .ToList();
            List<Cleaning> cleanings = await CleaningRepository.GetByBookingIdsAsync(departureIds);
            return bookings.ToDayOverview(date, cleanings);
        }
    }
}
