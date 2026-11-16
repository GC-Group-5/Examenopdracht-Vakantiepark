using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Vakantiepark.Infrastructure.Persistence;

namespace Vakantiepark.IntegrationTests
{
    public class PostgresFixture : IAsyncLifetime
    {
        private readonly PostgreSqlContainer Container = new PostgreSqlBuilder().WithImage("postgres:17").Build();

        public async Task InitializeAsync()
        {
            await Container.StartAsync();
            await using ApplicationDBContext context = CreateContext();
            await context.Database.MigrateAsync();
        }

        public Task DisposeAsync() => Container.DisposeAsync().AsTask();

        // Iedere aanroep is een eigen context, net als twee losse requests.
        public ApplicationDBContext CreateContext()
        {
            DbContextOptionsBuilder<ApplicationDBContext> options = new DbContextOptionsBuilder<ApplicationDBContext>();
            options.UseDatabase(Container.GetConnectionString());
            return new ApplicationDBContext(options.Options);
        }
    }
}