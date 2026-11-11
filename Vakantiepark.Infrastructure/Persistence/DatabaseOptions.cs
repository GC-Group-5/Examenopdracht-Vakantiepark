using Microsoft.EntityFrameworkCore;

namespace Vakantiepark.Infrastructure.Persistence
{
    public static class DatabaseOptions
    {
        // Used at runtime and by the EF tools, so both always use the same provider settings.
        public static DbContextOptionsBuilder UseDatabase(this DbContextOptionsBuilder builder, string connectionString)
        {
            return builder
                .UseNpgsql(connectionString, options => options.MigrationsHistoryTable("migrations"))
                .UseSnakeCaseNamingConvention();
        }
    }
}
