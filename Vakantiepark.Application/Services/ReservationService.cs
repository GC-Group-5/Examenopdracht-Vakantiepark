using Vakantiepark.Application.Extensions;
using Vakantiepark.Application.Helpers;
using Vakantiepark.Application.Interfaces;
using Vakantiepark.Application.Validations.Reservations;
using Vakantiepark.Domain.Entities;
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
        private readonly TimeProvider TimeProvider;

        public ReservationService(IBookingRepository bookingRepository, TimeProvider timeProvider)
        {
            BookingRepository = bookingRepository;
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

            DateOnly today = Today();
            PagedResult<Booking> result = await BookingRepository.SearchAsync(filter, today, request.Page, request.PageSize);

            return result.ToPagedResponse(request, booking => booking.ToReservationListItem(BookingStatus.Get(booking, today)));
        }

        public async Task<ReservationDetailResponse> GetByIdAsync(Guid id)
        {
            Booking booking = await BookingRepository.GetByIdWithDetailsAsync(id)
                ?? throw new KeyNotFoundException(NotFoundMessage);

            ReservationDetailResponse response = booking.ToMyBookingResponse<ReservationDetailResponse>(BookingStatus.Get(booking, Today()));
            response.Id = booking.Id;
            response.UnitCode = booking.AccommodationUnit.Code;
            response.CreatedAt = booking.CreatedAt;
            response.CancellationFee = booking.CancellationFee;
            return response;
        }

        private DateOnly Today()
            => DateOnly.FromDateTime(TimeProvider.GetUtcNow().UtcDateTime);
    }
}
