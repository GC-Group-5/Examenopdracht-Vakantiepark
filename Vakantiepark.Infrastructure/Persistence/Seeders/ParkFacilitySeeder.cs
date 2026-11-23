using Microsoft.EntityFrameworkCore;
using Vakantiepark.Domain.Entities;
using Vakantiepark.Domain.Enums;

namespace Vakantiepark.Infrastructure.Persistence.Seeders
{
    // Start data for the park information in the PWA (US-20).
    // Test data: the client has not supplied real facilities or photos yet, so all use the template photo.
    public static class ParkFacilitySeeder
    {
        private const string PhotoUrl = "img/placeholder-image.svg";
        private const string ParkTimeZoneId = "Europe/Amsterdam";

        public static async Task SeedParkFacilitiesAsync(this ApplicationDBContext context, CancellationToken cancellationToken = default)
        {
            DateTime now = DateTime.UtcNow;

            if (!await context.ParkFacilities.AnyAsync(facility => facility.Category != ParkFacilityCategory.Activity, cancellationToken))
                context.ParkFacilities.AddRange(
                    Facility("Brasserie De Waterkant", "Lunch, diner en een terras aan het water.", ParkFacilityCategory.FoodAndDrink, "Aan het meer", new TimeOnly(11, 0), new TimeOnly(22, 0), now),
                    Facility("Snackbar 't Hoekje", "Friet, snacks en ijs om mee te nemen.", ParkFacilityCategory.FoodAndDrink, "Centraal plein", new TimeOnly(12, 0), new TimeOnly(20, 0), now),
                    Facility("Parkwinkel", "Verse broodjes, boodschappen en vergeten spullen.", ParkFacilityCategory.Supermarket, "Naast de receptie", new TimeOnly(8, 0), new TimeOnly(18, 0), now),
                    Facility("Receptie", "Voor vragen, sleutels en informatie over het park.", ParkFacilityCategory.Facility, "Bij de ingang", new TimeOnly(8, 30), new TimeOnly(18, 0), now),
                    Facility("Wasserette", "Wasmachines en drogers, betalen met pin.", ParkFacilityCategory.Facility, "Naast de receptie", new TimeOnly(7, 0), new TimeOnly(22, 0), now),
                    Facility("Speeltuin", "Klimrekken, schommels en een zandbak voor de kleinsten.", ParkFacilityCategory.Facility, "Bosrand", new TimeOnly(9, 0), new TimeOnly(20, 0), now));

            // Activities are dated, so a new set is added whenever none is upcoming; the agenda is never empty in a demo.
            if (!await context.ParkFacilities.AnyAsync(facility => facility.Category == ParkFacilityCategory.Activity && facility.EndsAt > now, cancellationToken))
                context.ParkFacilities.AddRange(BuildActivities(now));

            await context.SaveChangesAsync(cancellationToken);
        }

        // Times are Dutch park times, stored as UTC like every other DateTime.
        public static List<ParkFacility> BuildActivities(DateTime now)
        {
            TimeZoneInfo parkTimeZone = TimeZoneInfo.FindSystemTimeZoneById(ParkTimeZoneId);
            DateTime parkToday = TimeZoneInfo.ConvertTimeFromUtc(now, parkTimeZone).Date;
            DateTime ParkTimeToUtc(int days, int hour, int minute) => TimeZoneInfo.ConvertTimeToUtc(parkToday.AddDays(days).AddHours(hour).AddMinutes(minute), parkTimeZone);

            return
            [
                Activity("Speurtocht door het bos", "Zoek de aanwijzingen en vind de schat. Voor kinderen van 6 tot 12 jaar.", "Start bij de speeltuin", ParkTimeToUtc(1, 10, 0), 90, now),
                Activity("Bingoavond", "Gezellige bingo met mooie prijzen voor jong en oud.", "Brasserie De Waterkant", ParkTimeToUtc(2, 19, 30), 120, now),
                Activity("Fietstocht door de Achterhoek", "Een rustige tocht van 25 km met een gids. Fietsen te huur bij de receptie.", "Start bij de receptie", ParkTimeToUtc(4, 13, 0), 180, now),
            ];
        }

        private static ParkFacility Facility(string name, string description, ParkFacilityCategory category, string location, TimeOnly opensAt, TimeOnly closesAt, DateTime now)
            => new() { Id = Guid.NewGuid(), Name = name, Description = description, Category = category, Location = location, PhotoUrl = PhotoUrl, OpensAt = opensAt, ClosesAt = closesAt, CreatedAt = now };

        private static ParkFacility Activity(string name, string description, string location, DateTime startsAt, int minutes, DateTime now)
            => new() { Id = Guid.NewGuid(), Name = name, Description = description, Category = ParkFacilityCategory.Activity, Location = location, PhotoUrl = PhotoUrl, StartsAt = startsAt, EndsAt = startsAt.AddMinutes(minutes), CreatedAt = now };
    }
}
