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
            services.AddScoped<IAccommodationService, AccommodationService>();
            services.AddScoped<IBookingService, BookingService>();
            services.AddScoped<IPriceService, PriceService>();
            services.AddScoped<IAccommodationSearchService, AccommodationSearchService>();
            services.AddScoped<IMaintenancePeriodService, MaintenancePeriodService>();
            services.AddScoped<IReservationService, ReservationService>();
            services.AddScoped<IParkFacilityService, ParkFacilityService>();
            services.AddScoped<IOccupancyService, OccupancyService>();
            services.AddScoped<IParkSettingsService, ParkSettingsService>();
            services.AddScoped<ISwimSlotService, SwimSlotService>();

            return services;
        }
    }
}
