using Vakantiepark.Shared.DTOs;
using Xunit;

namespace Vakantiepark.UnitTests
{
    public class ApiResponseTests
    {
        [Fact]
        public void Ok_WithValue_SucceedsAndKeepsValue()
        {
            ApiResponse<int> response = ApiResponse<int>.Ok(42);

            Assert.True(response.Succeeded);
            Assert.Equal(42, response.Value);
        }

        [Fact]
        public void Fail_WithMessage_DoesNotSucceed()
        {
            ApiResponse<int> response = ApiResponse<int>.Fail("Er ging iets mis.");

            Assert.False(response.Succeeded);
            Assert.Equal("Er ging iets mis.", response.Message);
        }

        [Fact]
        public void Error_WithErrors_ContainsEveryError()
        {
            List<ApiErrors> errors = [ApiErrors.Build("Email", ["Het e-mailadres is ongeldig."])];

            ApiResponse<object> response = ApiResponse<object>.Error(errors);

            Assert.False(response.Succeeded);
            Assert.Single(response.Errors);
        }
    }
}
