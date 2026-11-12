using Vakantiepark.Infrastructure.Persistence.Seeders;
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
        public static IServiceCollection RegisterInfrastructure(this IServiceCollection services, string connectionString, bool seedTestData)
        {
            services.RegisterPersistence(connectionString, seedTestData);
            services.RegisterRepositories();
            services.RegisterHelpers();

            return services;
        }

        private static IServiceCollection RegisterPersistence(this IServiceCollection services, string connectionString, bool seedTestData)
        {
            services.AddDbContext<ApplicationDBContext>(options =>
            {
                options.UseDatabase(connectionString);

                // Runs after the migrations in Program.cs; test accounts never end up outside development.
                if (seedTestData)
                    options.UseSeeding((context, _) => context.SeedTestUsers());
            });

            return services;
        }

        private static IServiceCollection RegisterRepositories(this IServiceCollection services)
        {
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped<IBookingRepository, BookingRepository>();
            services.AddScoped<IAccommodationUnitRepository, AccommodationUnitRepository>();
            services.AddScoped<IFeatureRepository, FeatureRepository>();
            services.AddScoped<IUserRepository, UserRepository>();

            return services;
        }

        private static IServiceCollection RegisterHelpers(this IServiceCollection services)
        {
            services.AddSingleton<IPasswordHasher, PasswordHasher>();
            services.AddSingleton<IBookingNumberGenerator, BookingNumberGenerator>();

            return services;
        }
    }
}
