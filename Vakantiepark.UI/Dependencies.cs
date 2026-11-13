using Microsoft.AspNetCore.Components.Authorization;
using Vakantiepark.UI.Application.Interfaces;
using Vakantiepark.UI.Application.Services;
using Vakantiepark.UI.Authentication;

namespace Vakantiepark.UI
{
    public static class Dependencies
    {
        public static IServiceCollection RegisterDependencies(this IServiceCollection services, IConfiguration configuration)
        {
            services.RegisterAuthentication();
            services.RegisterHttpClient(configuration);
            services.RegisterServices();

            return services;
        }

        private static IServiceCollection RegisterAuthentication(this IServiceCollection services)
        {
            services.AddScoped<TokenStorage>();
            services.AddScoped<JwtAuthenticationStateProvider>();
            services.AddScoped<AuthenticationStateProvider>(provider => provider.GetRequiredService<JwtAuthenticationStateProvider>());
            services.AddTransient<AuthTokenHandler>();

            services.AddAuthorizationCore();
            services.AddCascadingAuthenticationState();

            return services;
        }

        private static IServiceCollection RegisterHttpClient(this IServiceCollection services, IConfiguration configuration)
        {
            string baseUrl = configuration.GetValue<string>("ApiBaseUrl")
                ?? throw new InvalidOperationException("The setting 'ApiBaseUrl' was not found.");

            services.AddHttpClient<IHttpService, HttpService>(client => client.BaseAddress = new Uri(baseUrl))
                .AddHttpMessageHandler<AuthTokenHandler>();

            return services;
        }

        private static IServiceCollection RegisterServices(this IServiceCollection services)
        {
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IBookingService, BookingService>();

            return services;
        }
    }
}
