using Vakantiepark.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Vakantiepark.API.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class ErrorController : ControllerBase
    {
        [Route("/error")]
        public IActionResult HandleError()
        {
            return StatusCode(StatusCodes.Status500InternalServerError, ApiResponse<object>.Fail("Er is een onverwachte fout opgetreden."));
        }
    }
}
