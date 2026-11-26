using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Vakantiepark.API.Controllers;
using Vakantiepark.Shared.DTOs;
using Xunit;

namespace Vakantiepark.UnitTests
{
    // US-45: every controller runs its service call through RunAsync, which gives the status code and the message.
    public class ApiControllerBaseTests
    {
        private readonly TestController _sut = new TestController();

        private sealed class TestController : ApiControllerBase
        {
            public Task<IActionResult> Run<T>(Func<Task<T>> action, string? message = null) => RunAsync(action, message);

            public Task<IActionResult> Run(Func<Task> action, string? message = null) => RunAsync(action, message);
        }

        [Fact]
        public async Task RunAsync_Succeeds_ReturnsOkWithTheValue()
        {
            IActionResult result = await _sut.Run(() => Task.FromResult("Eik Luxe 6"));

            ApiResponse<string> response = Assert.IsType<ApiResponse<string>>(Assert.IsType<OkObjectResult>(result).Value);
            Assert.True(response.Succeeded);
            Assert.Equal("Eik Luxe 6", response.Value);
        }

        [Fact]
        public async Task RunAsync_WithoutValue_ReturnsOkWithTheMessage()
        {
            IActionResult result = await _sut.Run(() => Task.CompletedTask, "Het seizoen is opgeslagen.");

            ApiResponse<object?> response = Assert.IsType<ApiResponse<object?>>(Assert.IsType<OkObjectResult>(result).Value);
            Assert.True(response.Succeeded);
            Assert.Equal("Het seizoen is opgeslagen.", response.Message);
        }

        [Theory]
        [InlineData(typeof(ArgumentException), StatusCodes.Status400BadRequest)]
        [InlineData(typeof(KeyNotFoundException), StatusCodes.Status404NotFound)]
        [InlineData(typeof(UnauthorizedAccessException), StatusCodes.Status401Unauthorized)]
        public async Task RunAsync_KnownException_ReturnsStatusWithTheMessageOfTheService(Type exceptionType, int expectedStatusCode)
        {
            Exception exception = (Exception)Activator.CreateInstance(exceptionType, "Deze periode is niet meer vrij.")!;

            IActionResult result = await _sut.Run<string>(() => throw exception);

            ObjectResult objectResult = Assert.IsAssignableFrom<ObjectResult>(result);
            ApiResponse<string> response = Assert.IsType<ApiResponse<string>>(objectResult.Value);
            Assert.Equal(expectedStatusCode, objectResult.StatusCode);
            Assert.False(response.Succeeded);
            Assert.Equal("Deze periode is niet meer vrij.", response.Message);
        }

        // A programming error must not show its technical message to the user; /error makes it a 500.
        [Fact]
        public async Task RunAsync_UnexpectedException_IsNotCaught()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _sut.Run<string>(() => throw new InvalidOperationException("Sequence contains no elements")));
        }
    }
}
