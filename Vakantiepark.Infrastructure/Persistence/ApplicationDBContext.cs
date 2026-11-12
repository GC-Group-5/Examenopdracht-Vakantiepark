using Vakantiepark.Infrastructure.Persistence.Configurations;
using Vakantiepark.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Vakantiepark.Infrastructure.Persistence
{
    public class ApplicationDBContext : DbContext
    {
        public DbSet<AccommodationType> AccommodationTypes => Set<AccommodationType>();
        public DbSet<Accommodation> Accommodations => Set<Accommodation>();
        public DbSet<Booking> Bookings => Set<Booking>();

        public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Needed for the exclusion constraint that blocks overlapping bookings of one unit.
            builder.HasPostgresExtension("btree_gist");

            builder.Entity<AccommodationType>(options => options.Configure());
            builder.Entity<Accommodation>(options => options.Configure());
            builder.Entity<Booking>(options => options.Configure());
        }
    }
}
