using Microsoft.AspNetCore.Authentication.JwtBearer;
using Vakantiepark.Application.Interfaces;
using Vakantiepark.Application.Settings;
using System.Threading.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Vakantiepark.API.Converters;
using Vakantiepark.API.Extensions;
using Vakantiepark.Shared.Constants;
using Microsoft.AspNetCore.Mvc;
using Vakantiepark.Shared.DTOs;
using System.Security.Claims;
using Microsoft.OpenApi;
using System.Text;

namespace Vakantiepark.API
{
    public static class Dependencies
    {
        private const int RequestsPerMinute = 100;
        private const int LoginAttemptsPerMinute = 10;

        public static IServiceCollection RegisterAPI(this IServiceCollection services, JwtSettings jwtSettings)
        {
            services.RegisterAuthentication(jwtSettings);
            services.RegisterRateLimiter();
            services.RegisterSwagger();
            services.RegisterControllers();

            return services;
        }

        private static IServiceCollection RegisterAuthentication(this IServiceCollection services, JwtSettings jwtSettings)
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
                    ClockSkew = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents()
                {
                    // A token stays valid after an account is deactivated or a booking is cancelled, so both are checked on every request.
                    OnTokenValidated = async context =>
                    {
                        if (context.Principal is null || !await IsStillActiveAsync(context.Principal, context.HttpContext))
                            context.Fail("The account or booking is no longer active.");
                    },

                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsJsonAsync(ApiResponse<object>.Fail("Je bent niet ingelogd of je sessie is verlopen. Log opnieuw in."));
                    },

                    OnForbidden = async context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        await context.Response.WriteAsJsonAsync(ApiResponse<object>.Fail("Je hebt geen toegang tot deze functie."));
                    }
                };
            });

            services.AddAuthorization();

            return services;
        }

        private static async Task<bool> IsStillActiveAsync(ClaimsPrincipal principal, HttpContext httpContext)
        {
            IAuthService authService = httpContext.RequestServices.GetRequiredService<IAuthService>();

            if (principal.IsInRole(Roles.Gast))
                return principal.GetBookingId() is Guid bookingId && await authService.IsActiveBookingAsync(bookingId);

            return principal.GetUserId() is Guid userId && await authService.IsActiveUserAsync(userId);
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

                // Stricter limit on the login endpoints against guessing passwords and booking numbers.
                options.AddPolicy(RateLimitPolicies.Login, context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                        partition => new FixedWindowRateLimiterOptions()
                        {
                            PermitLimit = LoginAttemptsPerMinute,
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

                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "Plak hier de token uit /Auth/Login of /Auth/Guest."
                });

                options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", document)] = []
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
                    List<ApiErrors> errors = context.ModelState
                        .Where(x => x.Value is not null && x.Value.Errors.Count > 0)
                        .Select(x => ApiErrors.Build(x.Key, x.Value!.Errors.Select(e => e.ErrorMessage).ToList()))
                        .ToList();

                    return new BadRequestObjectResult(ApiResponse<object>.Error(errors));
                };
            });

            return services;
        }
    }
}
