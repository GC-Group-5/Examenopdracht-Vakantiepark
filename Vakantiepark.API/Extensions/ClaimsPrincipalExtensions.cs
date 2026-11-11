using Vakantiepark.Shared.Constants;
using System.Security.Claims;

namespace Vakantiepark.API.Extensions
{
    // Ids always come from the token, never from the request body or route, so a user can only act on their own data.
    public static class ClaimsPrincipalExtensions
    {
        public static Guid? GetUserId(this ClaimsPrincipal user)
            => Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out Guid id) ? id : null;

        public static Guid? GetBookingId(this ClaimsPrincipal user)
            => Guid.TryParse(user.FindFirstValue(ClaimNames.BookingId), out Guid id) ? id : null;
    }
}
