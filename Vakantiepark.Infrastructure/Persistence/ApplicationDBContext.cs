using Vakantiepark.Infrastructure.Persistence.Configurations;
using Vakantiepark.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Vakantiepark.Domain.Entities;
using Vakantiepark.Infrastructure.Persistence.Configurations;

namespace Vakantiepark.Infrastructure.Persistence
{
    public class ApplicationDBContext : DbContext
    {
        public DbSet<AccommodationType> AccommodationTypes => Set<AccommodationType>();
        public DbSet<Accommodation> Accommodations => Set<Accommodation>();

        public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options)
            : base(options) { }

        public DbSet<Booking> Bookings => Set<Booking>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<AccommodationType>(options => options.Configure());
            builder.Entity<Accommodation>(options => options.Configure());
        }
    }
}
