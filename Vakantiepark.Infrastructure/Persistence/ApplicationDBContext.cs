using Vakantiepark.Infrastructure.Persistence.Configurations;
using Vakantiepark.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Vakantiepark.Infrastructure.Persistence
{
    public class ApplicationDBContext : DbContext
    {
        public DbSet<AccommodationType> AccommodationTypes => Set<AccommodationType>();
        public DbSet<Accommodation> Accommodations => Set<Accommodation>();
        public DbSet<Zone> Zones => Set<Zone>();
        public DbSet<Feature> Features => Set<Feature>();
        public DbSet<FeatureOption> FeatureOptions => Set<FeatureOption>();
        public DbSet<AccommodationFeature> AccommodationFeatures => Set<AccommodationFeature>();

        public DbSet<Booking> Bookings => Set<Booking>();
        public DbSet<BookingGuest> BookingGuests => Set<BookingGuest>();

        public DbSet<ParkSettings> ParkSettings => Set<ParkSettings>();
        public DbSet<TouristTaxRate> TouristTaxRates => Set<TouristTaxRate>();
        public DbSet<Season> Seasons => Set<Season>();
        public DbSet<AccommodationTypePrice> AccommodationTypePrices => Set<AccommodationTypePrice>();
        public DbSet<User> Users => Set<User>();

        public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Needed for the exclusion constraint that blocks overlapping bookings of one unit.
            builder.HasPostgresExtension("btree_gist");

            builder.Entity<AccommodationType>(options => options.Configure());
            builder.Entity<Accommodation>(options => options.Configure());
            builder.Entity<Zone>(options => options.Configure());
            builder.Entity<Feature>(options => options.Configure());
            builder.Entity<FeatureOption>(options => options.Configure());
            builder.Entity<AccommodationFeature>(options => options.Configure());
            builder.Entity<Booking>(options => options.Configure());
            builder.Entity<BookingGuest>(options => options.Configure());
            builder.Entity<ParkSettings>(options => options.Configure());
            builder.Entity<TouristTaxRate>(options => options.Configure());
            builder.Entity<Season>(options => options.Configure());
            builder.Entity<AccommodationTypePrice>(options => options.Configure());
            builder.Entity<User>(options => options.Configure());
        }
    }
}
