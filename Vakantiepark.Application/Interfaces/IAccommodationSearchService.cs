using Vakantiepark.Shared.DTOs.Accommodation;

namespace Vakantiepark.Application.Interfaces
{
    public interface IAccommodationSearchService
    {
        Task<AccommodationSearchResponse> SearchAsync(string? accommodationTypeSlug, DateOnly arrivalDate, DateOnly departureDate, int numberOfGuests, int numberOfPets, Guid? zoneId);
    }
}
