using Vakantiepark.Domain.Interfaces.Repositories;
using Vakantiepark.Domain.Interfaces.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Vakantiepark.Infrastructure.Persistence;
using Vakantiepark.Infrastructure.Repositories;
using Vakantiepark.Infrastructure.Helpers;

namespace Vakantiepark.Infrastructure
{
    public static class Dependencies
    {
        public static IServiceCollection RegisterInfrastructure(this IServiceCollection services, string connectionString)
        {
            services.RegisterPersistence(connectionString);
            services.RegisterRepositories();
            services.RegisterHelpers();

            return services;
        }

        private static IServiceCollection RegisterPersistence(this IServiceCollection services, string connectionString)
        {
            services.AddDbContext<ApplicationDBContext>(options => options.UseDatabase(connectionString));

            return services;
        }

        private static IServiceCollection RegisterRepositories(this IServiceCollection services)
        {
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped<IBookingRepository, BookingRepository>();

            return services;
        }

        private static IServiceCollection RegisterHelpers(this IServiceCollection services)
        {
            services.AddSingleton<IBookingNumberGenerator, BookingNumberGenerator>();

            return services;
        }
    }
}
