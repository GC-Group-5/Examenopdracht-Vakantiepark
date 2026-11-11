using System.Threading.RateLimiting;
using Vakantiepark.API.Converters;
using Microsoft.AspNetCore.Mvc;
using Vakantiepark.Shared.DTOs;
using Microsoft.OpenApi;

namespace Vakantiepark.API
{
    public static class Dependencies
    {
        private const int RequestsPerMinute = 100;

        public static IServiceCollection RegisterAPI(this IServiceCollection services)
        {
            services.RegisterRateLimiter();
            services.RegisterSwagger();
            services.RegisterControllers();

            return services;
        }

        private static IServiceCollection RegisterRateLimiter(this IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                        partition => new FixedWindowRateLimiterOptions()
                        {
                            PermitLimit = RequestsPerMinute,
                            AutoReplenishment = true,
                            Window = TimeSpan.FromMinutes(1)
                        }));

                options.OnRejected = async (context, cancellationToken) =>
                {
                    context.HttpContext.Response.ContentType = "application/json";

                    ApiResponse<object> response = ApiResponse<object>.Fail("Je hebt te veel verzoeken gedaan. Probeer het over een minuut opnieuw.");
                    await context.HttpContext.Response.WriteAsJsonAsync(response, cancellationToken);
                };
            });

            return services;
        }

        private static IServiceCollection RegisterSwagger(this IServiceCollection services)
        {
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("api", new OpenApiInfo
                {
                    Title = "Vakantiepark D'r op Uut API",
                    Version = "1"
                });
            });

            return services;
        }

        private static IServiceCollection RegisterControllers(this IServiceCollection services)
        {
            IMvcBuilder builder = services.AddControllers();

            builder.AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new UtcDateTimeJsonConverter());
            });

            // Invalid request bodies get the same ApiResponse envelope as every other response.
            builder.ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    List<ApiErrors> errors = [.. context.ModelState
                        .Where(x => x.Value is not null && x.Value.Errors.Count > 0)
                        .Select(x => ApiErrors.Build(x.Key, [.. x.Value!.Errors.Select(e => e.ErrorMessage)]))];

                    return new BadRequestObjectResult(ApiResponse<object>.Error(errors));
                };
            });

            return services;
        }
    }
}
