using Microsoft.AspNetCore.Mvc;
using Vakantiepark.Shared.DTOs;

namespace Vakantiepark.API.Controllers
{
    // Services throw these with a message meant for the user (AGENTS.md §3). They are caught here, in our
    // own code, so the debugger does not stop on them; any other exception still goes to /error as a 500.
    public abstract class ApiControllerBase : ControllerBase
    {
        protected async Task<IActionResult> RunAsync<T>(Func<Task<T>> action, string? message = null)
        {
            try
            {
                T value = await action();
                return Ok(ApiResponse<T>.Ok(value, message));
            }
            catch (KeyNotFoundException exception)
            {
                return NotFound(ApiResponse<T>.Fail(exception.Message));
            }
            catch (UnauthorizedAccessException exception)
            {
                return Unauthorized(ApiResponse<T>.Fail(exception.Message));
            }
            catch (ArgumentException exception)
            {
                return BadRequest(ApiResponse<T>.Fail(exception.Message));
            }
        }

        // For an action without a value to return, such as saving or ending something.
        protected async Task<IActionResult> RunAsync(Func<Task> action, string? message = null)
            => await RunAsync<object?>(async () =>
            {
                await action();
                return null;
            }, message);
    }
}
