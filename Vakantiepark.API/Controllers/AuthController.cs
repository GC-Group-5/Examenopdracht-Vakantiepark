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
    public class AuthController : ControllerBase
    {
        private readonly IAuthService AuthService;

        public AuthController(IAuthService authService)
        {
            AuthService = authService;
        }

        [HttpPost("Login")]
        [EnableRateLimiting(RateLimitPolicies.Login)]
        public async Task<IActionResult> LoginAsync(LoginRequest request)
        {
            try
            {
                TokenResponse token = await AuthService.LoginAsync(request);
                return Ok(ApiResponse<TokenResponse>.Ok(token));
            }
            catch (UnauthorizedAccessException exception)
            {
                return Unauthorized(ApiResponse<TokenResponse>.Fail(exception.Message));
            }
        }

        [HttpPost("Guest")]
        [EnableRateLimiting(RateLimitPolicies.Login)]
        public async Task<IActionResult> GuestLoginAsync(GuestLoginRequest request)
        {
            try
            {
                TokenResponse token = await AuthService.GuestLoginAsync(request);
                return Ok(ApiResponse<TokenResponse>.Ok(token));
            }
            catch (UnauthorizedAccessException exception)
            {
                return Unauthorized(ApiResponse<TokenResponse>.Fail(exception.Message));
            }
        }

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
