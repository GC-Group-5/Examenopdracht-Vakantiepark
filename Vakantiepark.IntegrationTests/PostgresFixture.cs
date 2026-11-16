using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Vakantiepark.Infrastructure.Persistence;

namespace Vakantiepark.IntegrationTests
{
    // Starts PostgreSQL in Docker once per test class and applies all migrations, so tests run
    // against the real schema, including the exclusion constraint.
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

        // Every call is a new context, just like two separate requests.
        public ApplicationDBContext CreateContext()
        {
            DbContextOptionsBuilder<ApplicationDBContext> options = new DbContextOptionsBuilder<ApplicationDBContext>();
            options.UseDatabase(Container.GetConnectionString());
            return new ApplicationDBContext(options.Options);
        }
    }
}