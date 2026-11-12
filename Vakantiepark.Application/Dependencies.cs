using Microsoft.Extensions.DependencyInjection;
using Vakantiepark.Application.Interfaces;
using Vakantiepark.Application.Services;
using Vakantiepark.Application.Settings;

namespace Vakantiepark.Application
{
    public static class Dependencies
    {
        public static IServiceCollection RegisterApplication(this IServiceCollection services, JwtSettings jwtSettings)
        {
            services.AddSingleton(jwtSettings);
            services.AddSingleton(TimeProvider.System);

            services.RegisterServices();

            return services;
        }

        private static IServiceCollection RegisterServices(this IServiceCollection services)
        {
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IAuthService, AuthService>();
            //services.AddScoped<IBookingService, BookingService>();
            services.AddScoped<IAccommodationService, AccommodationService>();

            // US-06 and US-07 services: BookingService was added by the team with
            // US-07 interfaces; register both here so the controllers resolve.
            services.AddScoped<IBookingService, BookingService>();
            services.AddScoped<IPriceService, PriceService>();

            return services;
        }
    }
}
