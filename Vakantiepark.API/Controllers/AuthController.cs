using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Vakantiepark.Application.Interfaces;
using Vakantiepark.Shared.Constants;
using Vakantiepark.Shared.DTOs.Auth;
using Microsoft.AspNetCore.Mvc;
using Vakantiepark.Shared.DTOs;
using System.Security.Claims;

namespace Vakantiepark.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController : ApiControllerBase
    {
        private readonly IAuthService AuthService;

        public AuthController(IAuthService authService)
        {
            AuthService = authService;
        }

        [HttpPost("Login")]
        [EnableRateLimiting(RateLimitPolicies.Login)]
        public async Task<IActionResult> LoginAsync(LoginRequest request)
            => await RunAsync(() => AuthService.LoginAsync(request));

        [HttpPost("VerifyCode")]
        [EnableRateLimiting(RateLimitPolicies.Login)]
        public async Task<IActionResult> VerifyLoginCodeAsync(VerifyLoginCodeRequest request)
            => await RunAsync(() => AuthService.VerifyLoginCodeAsync(request));

        [HttpPost("Guest")]
        [EnableRateLimiting(RateLimitPolicies.Login)]
        public async Task<IActionResult> GuestLoginAsync(GuestLoginRequest request)
            => await RunAsync(() => AuthService.GuestLoginAsync(request));

        [HttpGet("Me")]
        [Authorize]
        public IActionResult GetCurrentUser()
        {
            CurrentUserResponse response = new CurrentUserResponse()
            {
                Name = User.FindFirstValue(ClaimTypes.Name) ?? string.Empty,
                Role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty,
                Email = User.FindFirstValue(ClaimTypes.Email),
                BookingNumber = User.FindFirstValue(ClaimNames.BookingNumber)
            };

            return Ok(ApiResponse<CurrentUserResponse>.Ok(response));
        }
    }
}
