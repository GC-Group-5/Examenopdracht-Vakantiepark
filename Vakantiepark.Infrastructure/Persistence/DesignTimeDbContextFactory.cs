using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore;

namespace Vakantiepark.Infrastructure.Persistence
{
    // Lets 'dotnet ef migrations add' run without Docker; creating a migration never connects to the database.
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDBContext>
    {
        public ApplicationDBContext CreateDbContext(string[] args)
        {
            DbContextOptionsBuilder<ApplicationDBContext> builder = new DbContextOptionsBuilder<ApplicationDBContext>();
            builder.UseDatabase("Host=localhost;Database=design_time;Username=postgres;Password=design_time");

            return new ApplicationDBContext(builder.Options);
        }
    }
}
