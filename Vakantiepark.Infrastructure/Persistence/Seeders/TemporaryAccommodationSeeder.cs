using Vakantiepark.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Vakantiepark.Domain.Enums;

namespace Vakantiepark.Infrastructure.Persistence.Seeders
{
    // TIJDELIJK: dummy accommodaties om de boekingsflow te testen. Weghalen vóór de pull request.
    public static class TemporaryAccommodationSeeder
    {
        public const string Slug = "chalet-4-personen";

        public static void SeedTemporaryAccommodations(this DbContext context)
        {
            DbSet<AccommodationType> types = context.Set<AccommodationType>();
            if (types.Any(x => x.Slug == Slug))
                return;

            DateTime now = DateTime.UtcNow;

            AccommodationType chalet = new AccommodationType()
            {
                Slug = Slug,
                Name = "Chalet 4 personen",
                Category = AccommodationCategory.HolidayHome,
                CreatedAt = now
            };

            chalet.Accommodations.Add(CreateChalet("C-01", "Chalet 1", now));
            chalet.Accommodations.Add(CreateChalet("C-02", "Chalet 2", now));

            types.Add(chalet);
            context.SaveChanges();
        }

        private static Accommodation CreateChalet(string code, string name, DateTime now)
            => new Accommodation()
            {
                Code = code,
                Name = name,
                Category = AccommodationCategory.HolidayHome,
                Zone = "Bos",
                MaxPersons = 4,
                Bedrooms = 2,
                Bathrooms = 1,
                Beds = 4,
                CreatedAt = now
            };
    }
}
