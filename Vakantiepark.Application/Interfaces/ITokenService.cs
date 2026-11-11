using Vakantiepark.Shared.DTOs.Auth;
using Vakantiepark.Domain.Entities;

namespace Vakantiepark.Application.Interfaces
{
    public interface ITokenService
    {
        TokenResponse CreateStaffToken(User user);
        TokenResponse CreateGuestToken(Booking booking);
    }
}
