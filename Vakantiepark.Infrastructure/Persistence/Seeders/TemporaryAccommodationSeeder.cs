using Vakantiepark.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Vakantiepark.Domain.Enums;

namespace Vakantiepark.Infrastructure.Persistence.Seeders
{
    // TIJDELIJK: dummy accommodaties om de boekingsflow te testen. Weghalen vóór de pull request.
    public static class TemporaryAccommodationSeeder
    {
        public static void SeedTemporaryAccommodations(this DbContext context)
        {
            SeedType(context, "chalet-4-personen", "Chalet 4 personen", maxPersons: 4, bedrooms: 2,
                ("C-01", "Chalet 1"), ("C-02", "Chalet 2"));

            SeedType(context, "bungalow-6-personen", "Bungalow 6 personen", maxPersons: 6, bedrooms: 3,
                ("B-01", "Bungalow 1"), ("B-02", "Bungalow 2"), ("B-03", "Bungalow 3"));
        }

        // Each type is checked on its own, so a type added later also lands in an existing database.
        private static void SeedType(DbContext context, string slug, string name, int maxPersons, int bedrooms, params (string Code, string Name)[] units)
        {
            DbSet<AccommodationType> types = context.Set<AccommodationType>();
            if (types.Any(x => x.Slug == slug))
                return;

            DateTime now = DateTime.UtcNow;

            AccommodationType type = new AccommodationType()
            {
                Slug = slug,
                Name = name,
                Category = AccommodationCategory.HolidayHome,
                CreatedAt = now
            };

            foreach ((string code, string unitName) in units)
            {
                type.Accommodations.Add(new Accommodation()
                {
                    Code = code,
                    Name = unitName,
                    Category = AccommodationCategory.HolidayHome,
                    Zone = "Bos",
                    MaxPersons = maxPersons,
                    Bedrooms = bedrooms,
                    Bathrooms = 1,
                    Beds = maxPersons,
                    CreatedAt = now
                });
            }

            types.Add(type);
            context.SaveChanges();
        }
    }
}
