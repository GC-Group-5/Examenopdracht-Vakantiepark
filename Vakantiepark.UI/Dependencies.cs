using Vakantiepark.UI.Application.Interfaces;
using Vakantiepark.UI.Application.Services;

namespace Vakantiepark.UI
{
    public static class Dependencies
    {
        public static IServiceCollection RegisterDependencies(this IServiceCollection services, IConfiguration configuration)
        {
            services.RegisterHttpClient(configuration);
            services.RegisterServices();

            return services;
        }

        private static IServiceCollection RegisterHttpClient(this IServiceCollection services, IConfiguration configuration)
        {
            string baseUrl = configuration.GetValue<string>("ApiBaseUrl")
                ?? throw new InvalidOperationException("The setting 'ApiBaseUrl' was not found.");

            services.AddHttpClient<IHttpService, HttpService>(client => client.BaseAddress = new Uri(baseUrl));

            return services;
        }

        private static IServiceCollection RegisterServices(this IServiceCollection services)
        {
            return services;
        }
    }
}
