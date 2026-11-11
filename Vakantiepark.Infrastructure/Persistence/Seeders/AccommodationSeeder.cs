using Microsoft.EntityFrameworkCore;
using Vakantiepark.Domain.Entities;
using Vakantiepark.Domain.Enums;

namespace Vakantiepark.Infrastructure.Persistence.Seeders
{
    // Seeddata converted one-time from the legacy export 'accommodaties_dr_op_uut.json'
    // (200 units, 12 types). The export is NOT parsed at runtime: it has been
    // converted to the tables below. Re-running the generator is a one-off task;
    // new units from the client arrive as new rows, keeping the seeder idempotent
    // by code (US-26: seeddata must be re-runnable without duplicates).
    public static class AccommodationSeeder
    {
        // Type table: slug, name, category, night rates per season index
        // (laag, midden, hoog, najaar) and the eindschoonmaak amount (US-05 test prices).
        private static readonly (string Slug, string Name, string Category, decimal[] NightRates, decimal CleaningFee)[] Types =
        [
            ("berk-4", "Berk 4", "holiday_home", [75m, 95m, 135m, 85m], 75m),
            ("berk-6", "Berk 6", "holiday_home", [90m, 115m, 160m, 105m], 90m),
            ("eik-6", "Eik 6", "holiday_home", [95m, 120m, 170m, 110m], 90m),
            ("eik-luxe-6", "Eik Luxe 6", "holiday_home", [120m, 150m, 210m, 140m], 110m),
            ("beuk-8", "Beuk 8", "holiday_home", [130m, 165m, 230m, 150m], 120m),
            ("wilg-4-mindervalide", "Wilg 4 Mindervalide", "holiday_home", [85m, 110m, 150m, 95m], 80m),
            ("bosvilla-10", "Bosvilla 10", "holiday_home", [175m, 220m, 310m, 200m], 150m),
            ("tentplaats-basis", "Tentplaats Basis", "camping_pitch", [15m, 18m, 25m, 17m], 0m),
            ("tentplaats-comfort", "Tentplaats Comfort", "camping_pitch", [18m, 22m, 30m, 20m], 0m),
            ("caravanplaats", "Caravanplaats", "camping_pitch", [20m, 25m, 35m, 23m], 0m),
            ("camperplaats", "Camperplaats", "camping_pitch", [22m, 27m, 38m, 25m], 0m),
            ("camperplaats-xl", "Camperplaats XL", "camping_pitch", [27m, 33m, 46m, 30m], 0m),
        ];

        // Homes: code, type, zone, maxPersons, bedrooms, bathrooms, beds, petsAllowed,
        // maxPets, wheelchairAccessible, sauna, dishwasher, washingMachine,
        // airConditioning, fireplace, coveredTerrace, chargingPoint, waterfront,
        // nearPool, childFriendly, smokeFree, hotTub, levelAccess, adaptedBathroom,
        // widenedDoors, adjustableBed, extraParkingSpace.
        private static readonly (string Code, string Type, string Zone, int MaxPersons, int Bedrooms, int Bathrooms, int Beds, bool Pets, int MaxPets, bool Wheelchair, bool Sauna, bool Dishwasher, bool WashingMachine, bool Airco, bool Fireplace, bool CoveredTerrace, bool ChargingPoint, bool Waterfront, bool NearPool, bool ChildFriendly, bool SmokeFree, bool HotTub, bool LevelAccess, bool AdaptedBathroom, bool WidenedDoors, bool AdjustableBed, bool ExtraParking)[] Homes =
        [
            ("BK4-001", "Berk 4", "Rustweide", 4, 2, 1, 4, true, 2, false, false, true, false, false, false, true, false, true, false, true, true, false, false, false, false, false, false),
            ("BK4-002", "Berk 4", "Waterkant", 4, 2, 1, 4, true, 2, false, false, false, true, true, false, true, false, true, true, true, true, false, false, false, false, false, false),
            ("BK4-003", "Berk 4", "Bosrand", 4, 2, 1, 4, false, 0, false, false, false, false, false, false, false, false, false, true, true, true, false, false, false, false, false, false),
            ("BK4-004", "Berk 4", "Parkcentrum", 4, 2, 1, 4, true, 2, false, false, true, false, false, false, true, false, false, true, false, true, false, false, false, false, false, false),
            ("BK4-005", "Berk 4", "Parkcentrum", 4, 2, 1, 4, true, 2, false, false, true, false, false, false, true, false, false, false, false, true, false, false, false, false, false, false),
            ("BK4-006", "Berk 4", "Heideveld", 4, 2, 1, 4, false, 0, false, false, true, true, false, false, false, true, false, true, false, true, false, false, false, false, false, false),
            ("BK4-007", "Berk 4", "Parkcentrum", 4, 2, 1, 4, false, 0, false, false, false, false, false, false, true, false, false, false, false, true, false, false, false, false, false, false),
            ("BK4-008", "Berk 4", "Parkcentrum", 4, 2, 1, 4, false, 0, false, false, false, false, true, true, true, false, true, false, false, true, false, false, false, false, false, false),
            ("BK4-009", "Berk 4", "Bosrand", 4, 2, 1, 4, false, 0, false, false, true, false, false, false, true, false, false, true, true, true, false, false, false, false, false, false),
            ("BK4-010", "Berk 4", "Waterkant", 4, 2, 1, 4, true, 2, false, false, true, true, false, false, true, false, false, false, true, true, false, false, false, false, false, false),
            ("BK4-011", "Berk 4", "Rustweide", 4, 2, 1, 4, false, 0, false, false, true, false, false, true, false, true, false, false, false, true, false, false, false, false, false, false),
            ("BK4-012", "Berk 4", "Heideveld", 4, 2, 1, 4, true, 2, false, false, true, true, false, false, false, false, true, false, true, true, false, false, false, false, false, false),
            ("BK4-013", "Berk 4", "Waterkant", 4, 2, 1, 4, true, 2, false, false, false, false, false, false, false, true, true, false, true, true, false, false, false, false, false, false),
            ("BK4-014", "Berk 4", "Parkcentrum", 4, 2, 1, 4, true, 2, false, false, true, false, false, false, false, false, false, false, true, true, false, false, false, false, false, false),
            ("BK4-015", "Berk 4", "Rustweide", 4, 2, 1, 4, false, 0, false, false, true, true, true, false, true, false, false, true, false, true, false, false, false, false, false, false),
            ("BK4-016", "Berk 4", "Rustweide", 4, 2, 1, 4, true, 2, false, false, true, false, true, true, false, false, false, true, true, true, false, false, false, false, false, false),
            ("BK4-017", "Berk 4", "Heideveld", 4, 2, 1, 4, true, 2, false, false, true, true, false, true, false, false, false, false, true, true, false, false, false, false, false, false),
            ("BK4-018", "Berk 4", "Parkcentrum", 4, 2, 1, 4, true, 2, false, false, true, true, false, false, false, false, true, true, false, true, false, false, false, false, false, false),
            ("BK4-019", "Berk 4", "Waterkant", 4, 2, 1, 4, false, 0, false, false, true, true, false, false, false, true, true, false, true, true, false, false, false, false, false, false),
            ("BK4-020", "Berk 4", "Bosrand", 4, 2, 1, 4, false, 0, false, false, true, false, false, false, false, true, false, false, false, true, false, false, false, false, false, false),
            ("BK4-021", "Berk 4", "Rustweide", 4, 2, 1, 4, true, 2, false, false, true, true, false, true, false, false, false, false, true, true, false, false, false, false, false, false),
            ("BK4-022", "Berk 4", "Heideveld", 4, 2, 1, 4, true, 2, false, false, true, false, false, false, false, false, false, false, false, true, false, false, false, false, false, false),
            ("BK4-023", "Berk 4", "Parkcentrum", 4, 2, 1, 4, true, 2, false, false, true, false, false, false, true, false, true, true, true, true, false, false, false, false, false, false),
            ("BK4-024", "Berk 4", "Heideveld", 4, 2, 1, 4, true, 2, false, false, false, false, false, true, false, false, false, false, false, true, false, false, false, false, false, false),
            ("BK4-025", "Berk 4", "Parkcentrum", 4, 2, 1, 4, false, 0, false, false, false, false, true, false, true, false, false, true, true, true, false, false, false, false, false, false),
            ("BK4-026", "Berk 4", "Heideveld", 4, 2, 1, 4, true, 2, true, false, true, false, false, true, false, false, false, true, true, true, false, false, false, false, false, false),
            ("BK4-027", "Berk 4", "Parkcentrum", 4, 2, 1, 4, true, 2, false, false, true, false, false, true, false, false, false, false, true, true, false, false, false, false, false, false),
            ("BK4-028", "Berk 4", "Rustweide", 4, 2, 1, 4, true, 2, true, false, true, false, false, false, true, false, false, false, true, true, false, false, false, false, false, false),
            ("BK4-029", "Berk 4", "Heideveld", 4, 2, 1, 4, true, 2, false, false, true, false, false, false, false, false, false, false, true, true, false, false, false, false, false, false),
            ("BK4-030", "Berk 4", "Heideveld", 4, 2, 1, 4, true, 2, false, false, true, false, true, false, false, false, true, true, false, true, false, false, false, false, false, false),
            ("BK4-031", "Berk 4", "Waterkant", 4, 2, 1, 4, false, 0, false, false, false, true, true, false, false, true, false, true, true, true, false, false, false, false, false, false),
            ("BK4-032", "Berk 4", "Parkcentrum", 4, 2, 1, 4, false, 0, false, false, false, false, false, false, true, false, true, false, true, true, false, false, false, false, false, false),
            ("BK6-001", "Berk 6", "Rustweide", 6, 3, 1, 6, true, 2, false, false, true, false, false, false, false, false, false, true, false, true, false, false, false, false, false, false),
            ("BK6-002", "Berk 6", "Heideveld", 6, 3, 1, 6, false, 0, false, false, true, true, false, true, true, false, false, true, true, true, false, false, false, false, false, false),
            ("BK6-003", "Berk 6", "Heideveld", 6, 3, 1, 6, false, 0, false, false, true, false, false, false, false, false, false, false, false, true, false, false, false, false, false, false),
            ("BK6-004", "Berk 6", "Parkcentrum", 6, 3, 1, 6, false, 0, false, false, true, true, false, false, true, false, false, true, true, true, false, false, false, false, false, false),
            ("BK6-005", "Berk 6", "Heideveld", 6, 3, 1, 6, false, 0, false, false, true, true, false, false, true, false, false, false, true, true, false, false, false, false, false, false),
            ("BK6-006", "Berk 6", "Rustweide", 6, 3, 1, 6, false, 0, false, false, true, false, false, false, true, false, false, false, true, true, false, false, false, false, false, false),
            ("BK6-007", "Berk 6", "Heideveld", 6, 3, 1, 6, false, 0, false, false, true, true, true, false, false, false, false, false, true, true, false, false, false, false, false, false),
            ("BK6-008", "Berk 6", "Bosrand", 6, 3, 1, 6, false, 0, false, false, true, true, true, false, true, true, false, true, true, true, false, false, false, false, false, false),
            ("BK6-009", "Berk 6", "Waterkant", 6, 3, 1, 6, true, 2, false, false, true, true, false, true, true, true, false, false, true, true, false, false, false, false, false, false),
            ("BK6-010", "Berk 6", "Parkcentrum", 6, 3, 1, 6, true, 2, false, false, true, false, false, false, true, false, false, false, true, true, false, false, false, false, false, false),
            ("BK6-011", "Berk 6", "Waterkant", 6, 3, 1, 6, false, 0, false, false, true, false, false, true, false, false, false, true, true, true, false, false, false, false, false, false),
            ("BK6-012", "Berk 6", "Heideveld", 6, 3, 1, 6, false, 0, false, false, true, true, true, false, false, false, false, false, true, true, false, false, false, false, false, false),
            ("BK6-013", "Berk 6", "Heideveld", 6, 3, 1, 6, false, 0, false, false, true, false, true, true, false, false, true, true, true, true, false, false, false, false, false, false),
            ("BK6-014", "Berk 6", "Bosrand", 6, 3, 1, 6, false, 0, false, false, true, true, false, false, false, false, false, false, true, true, false, false, false, false, false, false),
            ("BK6-015", "Berk 6", "Heideveld", 6, 3, 1, 6, false, 0, false, false, true, true, false, true, false, false, false, false, true, true, false, false, false, false, false, false),
            ("BK6-016", "Berk 6", "Parkcentrum", 6, 3, 1, 6, true, 2, false, false, true, false, false, false, false, false, false, false, true, true, false, false, false, false, false, false),
            ("BK6-017", "Berk 6", "Heideveld", 6, 3, 1, 6, true, 2, false, false, true, true, false, false, true, false, true, false, false, true, false, false, false, false, false, false),
            ("BK6-018", "Berk 6", "Rustweide", 6, 3, 1, 6, false, 0, false, false, true, true, false, true, false, true, false, false, true, true, false, false, false, false, false, false),
            ("BK6-019", "Berk 6", "Parkcentrum", 6, 3, 1, 6, true, 2, false, false, true, false, false, true, true, false, false, false, true, true, false, false, false, false, false, false),
            ("BK6-020", "Berk 6", "Waterkant", 6, 3, 1, 6, true, 2, false, false, true, true, false, false, true, true, true, false, true, true, false, false, false, false, false, false),
            ("BK6-021", "Berk 6", "Bosrand", 6, 3, 1, 6, true, 2, false, false, true, true, false, false, false, false, true, false, true, true, false, false, false, false, false, false),
            ("BK6-022", "Berk 6", "Waterkant", 6, 3, 1, 6, true, 2, false, false, true, true, false, true, false, true, false, true, true, true, false, false, false, false, false, false),
            ("BK6-023", "Berk 6", "Waterkant", 6, 3, 1, 6, false, 0, false, false, true, true, false, false, true, false, true, false, true, true, false, false, false, false, false, false),
            ("BK6-024", "Berk 6", "Waterkant", 6, 3, 1, 6, true, 2, false, false, true, false, false, false, true, true, true, true, false, true, false, false, false, false, false, false),
            ("BK6-025", "Berk 6", "Rustweide", 6, 3, 1, 6, true, 2, false, false, true, true, false, false, false, true, false, false, true, true, false, false, false, false, false, false),
            ("BK6-026", "Berk 6", "Parkcentrum", 6, 3, 1, 6, true, 2, false, false, true, false, false, false, true, false, true, false, true, true, false, false, false, false, false, false),
            ("EK6-001", "Eik 6", "Rustweide", 6, 3, 1, 6, false, 0, false, false, true, true, false, false, true, false, false, true, true, true, false, false, false, false, false, false),
            ("EK6-002", "Eik 6", "Heideveld", 6, 3, 2, 6, false, 0, false, false, true, true, true, true, true, true, true, false, false, true, false, false, false, false, false, false),
            ("EK6-003", "Eik 6", "Bosrand", 6, 3, 2, 6, true, 2, false, false, true, true, true, false, false, false, false, false, true, true, false, false, false, false, false, false),
            ("EK6-004", "Eik 6", "Bosrand", 6, 3, 1, 6, false, 0, false, false, true, false, false, true, false, false, false, true, true, true, false, false, false, false, false, false),
            ("EK6-005", "Eik 6", "Rustweide", 6, 3, 2, 6, false, 0, false, false, true, false, true, true, false, false, false, false, false, true, false, false, false, false, false, false),
            ("EK6-006", "Eik 6", "Heideveld", 6, 3, 1, 6, true, 2, false, false, true, true, true, true, true, false, false, false, false, true, false, false, false, false, false, false),
            ("EK6-007", "Eik 6", "Rustweide", 6, 3, 1, 6, true, 2, false, false, true, true, false, false, false, false, false, false, true, true, false, false, false, false, false, false),
            ("EK6-008", "Eik 6", "Rustweide", 6, 3, 2, 6, false, 0, false, true, true, true, true, false, false, true, false, true, true, true, false, false, false, false, false, false),
            ("EK6-009", "Eik 6", "Waterkant", 6, 3, 2, 6, false, 0, false, false, true, true, false, true, true, true, false, false, true, true, false, false, false, false, false, false),
            ("EK6-010", "Eik 6", "Rustweide", 6, 3, 1, 6, true, 2, false, false, true, true, false, false, true, false, false, true, true, true, false, false, false, false, false, false),
            ("EK6-011", "Eik 6", "Rustweide", 6, 3, 2, 6, false, 0, false, false, true, true, false, false, true, false, true, false, true, true, false, false, false, false, false, false),
            ("EK6-012", "Eik 6", "Parkcentrum", 6, 3, 1, 6, true, 2, false, false, true, false, true, false, false, false, true, true, true, true, false, false, false, false, false, false),
            ("EK6-013", "Eik 6", "Parkcentrum", 6, 3, 2, 6, true, 2, false, true, true, false, false, true, false, true, false, true, true, true, false, false, false, false, false, false),
            ("EK6-014", "Eik 6", "Parkcentrum", 6, 3, 2, 6, false, 0, false, false, true, false, false, false, false, true, false, true, true, true, false, false, false, false, false, false),
            ("EK6-015", "Eik 6", "Rustweide", 6, 3, 2, 6, true, 2, false, true, true, true, false, false, true, true, false, true, true, true, false, false, false, false, false, false),
            ("EK6-016", "Eik 6", "Waterkant", 6, 3, 1, 6, true, 2, false, true, true, true, false, false, false, false, false, true, true, true, false, false, false, false, false, false),
            ("EK6-017", "Eik 6", "Waterkant", 6, 3, 1, 6, true, 2, false, true, true, false, true, false, false, true, false, false, true, true, false, false, false, false, false, false),
            ("EK6-018", "Eik 6", "Bosrand", 6, 3, 2, 6, true, 2, false, false, true, true, false, false, true, false, false, false, true, true, false, false, false, false, false, false),
            ("EK6-019", "Eik 6", "Bosrand", 6, 3, 1, 6, true, 2, false, false, true, true, false, false, false, false, true, true, false, true, false, false, false, false, false, false),
            ("EK6-020", "Eik 6", "Heideveld", 6, 3, 1, 6, true, 2, false, true, true, false, false, true, true, true, false, false, false, true, false, false, false, false, false, false),
            ("EK6-021", "Eik 6", "Heideveld", 6, 3, 2, 6, true, 2, false, true, true, true, false, false, false, true, false, true, true, true, false, false, false, false, false, false),
            ("EK6-022", "Eik 6", "Rustweide", 6, 3, 2, 6, false, 0, false, false, true, false, false, true, true, false, false, false, true, true, false, false, false, false, false, false),
            ("EK6-023", "Eik 6", "Rustweide", 6, 3, 2, 6, false, 0, false, false, true, false, false, false, true, false, false, true, false, true, false, false, false, false, false, false),
            ("EK6-024", "Eik 6", "Rustweide", 6, 3, 1, 6, true, 2, false, false, true, true, false, true, false, false, true, true, true, true, false, false, false, false, false, false),
            ("EL6-001", "Eik Luxe 6", "Heideveld", 6, 3, 2, 6, true, 2, false, true, true, true, false, false, true, true, false, false, true, true, false, false, false, false, false, false),
            ("EL6-002", "Eik Luxe 6", "Rustweide", 6, 3, 2, 6, true, 2, true, false, true, true, false, true, true, false, true, true, false, true, false, false, false, false, false, false),
            ("EL6-003", "Eik Luxe 6", "Waterkant", 6, 3, 2, 6, false, 0, false, true, true, true, false, true, true, true, true, true, false, true, true, false, false, false, false, false),
            ("EL6-004", "Eik Luxe 6", "Waterkant", 6, 3, 2, 6, true, 2, false, true, true, true, true, true, true, true, false, false, true, true, false, false, false, false, false, false),
            ("EL6-005", "Eik Luxe 6", "Rustweide", 6, 3, 2, 6, false, 0, false, true, true, true, true, true, true, true, true, false, false, true, false, false, false, false, false, false),
            ("EL6-006", "Eik Luxe 6", "Waterkant", 6, 3, 2, 6, true, 2, false, true, true, true, true, true, true, true, false, true, false, true, false, false, false, false, false, false),
            ("EL6-007", "Eik Luxe 6", "Waterkant", 6, 3, 2, 6, true, 2, false, true, true, true, true, true, true, false, true, false, true, true, true, false, false, false, false, false),
            ("EL6-008", "Eik Luxe 6", "Parkcentrum", 6, 3, 2, 6, false, 0, false, true, true, true, true, true, true, true, false, true, true, true, true, false, false, false, false, false),
            ("EL6-009", "Eik Luxe 6", "Waterkant", 6, 3, 2, 6, false, 0, false, true, true, true, true, true, true, false, false, false, false, true, false, false, false, false, false, false),
            ("EL6-010", "Eik Luxe 6", "Rustweide", 6, 3, 2, 6, false, 0, false, true, true, true, true, false, true, false, false, false, true, true, false, false, false, false, false, false),
            ("EL6-011", "Eik Luxe 6", "Heideveld", 6, 3, 2, 6, true, 2, false, true, true, true, true, false, true, true, true, false, true, true, false, false, false, false, false, false),
            ("EL6-012", "Eik Luxe 6", "Rustweide", 6, 3, 2, 6, true, 2, false, true, true, true, true, false, true, true, false, false, true, true, true, false, false, false, false, false),
            ("EL6-013", "Eik Luxe 6", "Heideveld", 6, 3, 2, 6, false, 0, false, true, true, true, true, true, true, true, false, false, true, true, false, false, false, false, false, false),
            ("EL6-014", "Eik Luxe 6", "Bosrand", 6, 3, 2, 6, false, 0, false, true, true, true, true, true, true, false, false, false, true, true, false, false, false, false, false, false),
            ("EL6-015", "Eik Luxe 6", "Parkcentrum", 6, 3, 2, 6, true, 2, false, true, true, true, true, true, true, true, true, false, true, true, true, false, false, false, false, false),
            ("EL6-016", "Eik Luxe 6", "Rustweide", 6, 3, 2, 6, false, 0, false, true, true, true, true, true, true, true, false, true, true, true, false, false, false, false, false, false),
            ("EL6-017", "Eik Luxe 6", "Parkcentrum", 6, 3, 2, 6, false, 0, false, false, true, true, true, true, true, true, true, true, true, true, true, false, false, false, false, false),
            ("EL6-018", "Eik Luxe 6", "Bosrand", 6, 3, 2, 6, false, 0, false, true, true, true, true, true, true, false, true, true, true, true, true, false, false, false, false, false),
            ("BU8-001", "Beuk 8", "Parkcentrum", 8, 4, 3, 8, false, 0, false, true, true, true, false, true, false, false, false, false, false, true, false, false, false, false, false, false),
            ("BU8-002", "Beuk 8", "Waterkant", 8, 4, 3, 8, false, 0, false, false, true, true, true, false, false, false, true, false, true, true, false, false, false, false, false, false),
            ("BU8-003", "Beuk 8", "Parkcentrum", 8, 4, 2, 8, false, 0, false, false, true, false, true, true, false, true, false, false, true, true, false, false, false, false, false, false),
            ("BU8-004", "Beuk 8", "Parkcentrum", 8, 4, 2, 8, false, 0, false, false, true, false, true, true, true, true, false, false, true, true, false, false, false, false, false, false),
            ("BU8-005", "Beuk 8", "Bosrand", 8, 4, 3, 8, true, 2, false, true, true, true, false, true, true, false, true, true, false, true, false, false, false, false, false, false),
            ("BU8-006", "Beuk 8", "Bosrand", 8, 4, 2, 8, false, 0, true, false, true, true, false, false, false, false, false, false, true, true, false, false, false, false, false, false),
            ("BU8-007", "Beuk 8", "Heideveld", 8, 4, 3, 8, true, 2, false, false, true, true, false, false, true, false, true, true, true, true, false, false, false, false, false, false),
            ("BU8-008", "Beuk 8", "Rustweide", 8, 4, 2, 8, false, 0, false, false, true, true, true, true, true, true, false, false, true, true, false, false, false, false, false, false),
            ("BU8-009", "Beuk 8", "Bosrand", 8, 4, 2, 8, false, 0, false, false, true, true, false, false, false, true, false, false, false, true, false, false, false, false, false, false),
            ("BU8-010", "Beuk 8", "Rustweide", 8, 4, 3, 8, false, 0, false, false, true, true, true, true, true, false, false, true, true, true, false, false, false, false, false, false),
            ("BU8-011", "Beuk 8", "Heideveld", 8, 4, 3, 8, false, 0, false, false, true, true, false, true, false, false, false, true, true, true, false, false, false, false, false, false),
            ("BU8-012", "Beuk 8", "Heideveld", 8, 4, 2, 8, true, 2, false, false, true, false, false, false, true, false, false, false, true, true, false, false, false, false, false, false),
            ("BU8-013", "Beuk 8", "Heideveld", 8, 4, 2, 8, false, 0, false, false, true, true, true, false, false, true, false, false, true, true, false, false, false, false, false, false),
            ("BU8-014", "Beuk 8", "Heideveld", 8, 4, 3, 8, false, 0, false, false, true, true, true, false, true, false, false, false, true, true, false, false, false, false, false, false),
            ("BU8-015", "Beuk 8", "Bosrand", 8, 4, 2, 8, true, 2, false, false, true, true, true, true, false, false, false, false, false, true, false, false, false, false, false, false),
            ("BU8-016", "Beuk 8", "Bosrand", 8, 4, 3, 8, false, 0, false, true, true, true, true, false, false, false, true, true, true, true, false, false, false, false, false, false),
            ("BU8-017", "Beuk 8", "Waterkant", 8, 4, 3, 8, false, 0, false, true, true, true, true, false, true, false, false, false, true, true, false, false, false, false, false, false),
            ("BU8-018", "Beuk 8", "Parkcentrum", 8, 4, 3, 8, true, 2, false, false, true, true, false, false, false, true, false, false, true, true, false, false, false, false, false, false),
            ("BU8-019", "Beuk 8", "Heideveld", 8, 4, 2, 8, true, 2, false, false, true, true, false, true, true, false, true, false, true, true, false, false, false, false, false, false),
            ("BU8-020", "Beuk 8", "Bosrand", 8, 4, 3, 8, false, 0, true, false, true, true, false, true, false, true, true, false, true, true, false, false, false, false, false, false),
            ("WG4M-001", "Wilg 4 Mindervalide", "Waterkant", 4, 2, 1, 4, true, 2, true, false, true, true, true, false, false, false, false, false, true, true, false, true, true, true, true, false),
            ("WG4M-002", "Wilg 4 Mindervalide", "Waterkant", 4, 2, 1, 4, true, 2, true, false, true, true, true, false, true, false, false, true, true, true, false, true, true, true, true, false),
            ("WG4M-003", "Wilg 4 Mindervalide", "Bosrand", 4, 2, 1, 4, true, 2, true, false, true, false, false, false, true, true, false, true, true, true, false, true, true, true, false, false),
            ("WG4M-004", "Wilg 4 Mindervalide", "Rustweide", 4, 2, 1, 4, false, 0, true, false, true, true, true, false, true, false, false, false, true, true, false, true, true, true, true, false),
            ("WG4M-005", "Wilg 4 Mindervalide", "Bosrand", 4, 2, 1, 4, false, 0, true, false, true, true, false, false, false, false, false, true, true, true, false, true, true, true, false, false),
            ("WG4M-006", "Wilg 4 Mindervalide", "Rustweide", 4, 2, 1, 4, false, 0, true, false, true, false, true, false, true, false, false, true, true, true, false, true, true, true, true, false),
            ("WG4M-007", "Wilg 4 Mindervalide", "Bosrand", 4, 2, 1, 4, true, 2, true, false, true, true, true, false, true, false, false, true, true, true, false, true, true, true, true, false),
            ("WG4M-008", "Wilg 4 Mindervalide", "Rustweide", 4, 2, 1, 4, false, 0, true, false, true, false, false, false, true, false, false, false, true, true, false, true, true, true, true, false),
            ("WG4M-009", "Wilg 4 Mindervalide", "Parkcentrum", 4, 2, 1, 4, false, 0, true, false, true, true, true, false, true, false, true, true, true, true, false, true, true, true, true, false),
            ("WG4M-010", "Wilg 4 Mindervalide", "Bosrand", 4, 2, 1, 4, true, 2, true, false, true, false, false, false, true, false, false, false, true, true, false, true, true, true, true, false),
            ("WG4M-011", "Wilg 4 Mindervalide", "Parkcentrum", 4, 2, 1, 4, false, 0, true, false, true, false, true, false, true, false, false, true, true, true, false, true, true, true, true, false),
            ("WG4M-012", "Wilg 4 Mindervalide", "Rustweide", 4, 2, 1, 4, false, 0, true, false, true, false, true, false, true, true, false, true, true, true, false, true, true, true, true, false),
            ("BV10-001", "Bosvilla 10", "Rustweide", 10, 5, 2, 10, false, 0, false, false, true, true, true, true, true, false, true, true, true, true, false, false, false, false, false, false),
            ("BV10-002", "Bosvilla 10", "Rustweide", 10, 5, 3, 10, false, 0, false, true, true, true, true, true, true, true, false, false, true, true, false, false, false, false, false, true),
            ("BV10-003", "Bosvilla 10", "Heideveld", 10, 5, 3, 10, true, 2, false, true, true, true, false, true, true, false, true, false, true, true, false, false, false, false, false, false),
            ("BV10-004", "Bosvilla 10", "Heideveld", 10, 5, 3, 10, true, 2, false, true, true, true, false, true, true, true, false, false, true, true, false, false, false, false, false, false),
            ("BV10-005", "Bosvilla 10", "Bosrand", 10, 5, 3, 10, true, 2, false, true, true, true, true, true, true, false, true, false, true, true, false, false, false, false, false, true),
            ("BV10-006", "Bosvilla 10", "Bosrand", 10, 5, 3, 10, true, 2, false, true, true, true, false, true, true, true, true, false, true, true, false, false, false, false, false, true),
            ("BV10-007", "Bosvilla 10", "Rustweide", 10, 5, 2, 10, true, 2, false, true, true, true, true, true, false, true, true, false, true, true, false, false, false, false, false, true),
            ("BV10-008", "Bosvilla 10", "Waterkant", 10, 5, 2, 10, true, 2, false, true, true, true, true, false, true, false, false, false, true, true, false, false, false, false, false, true),
            ("BV10-009", "Bosvilla 10", "Rustweide", 10, 5, 2, 10, true, 2, false, true, true, true, true, true, true, true, false, true, true, true, false, false, false, false, false, true),
            ("BV10-010", "Bosvilla 10", "Rustweide", 10, 5, 2, 10, true, 2, false, false, true, true, true, true, true, false, false, false, true, true, true, false, false, false, false, true),
            ("BV10-011", "Bosvilla 10", "Bosrand", 10, 5, 3, 10, true, 2, false, true, true, true, false, true, false, true, false, true, true, true, false, false, false, false, false, true),
            ("BV10-012", "Bosvilla 10", "Bosrand", 10, 5, 2, 10, false, 0, false, true, true, true, true, true, true, true, true, true, true, true, false, false, false, false, false, true),
            ("BV10-013", "Bosvilla 10", "Rustweide", 10, 5, 2, 10, false, 0, false, false, true, true, false, true, true, true, false, false, true, true, true, false, false, false, false, true),
            ("BV10-014", "Bosvilla 10", "Parkcentrum", 10, 5, 2, 10, true, 2, false, true, true, true, true, true, true, false, false, false, true, true, false, false, false, false, false, false),
            ("BV10-015", "Bosvilla 10", "Parkcentrum", 10, 5, 3, 10, true, 2, false, true, true, true, true, true, true, true, false, true, true, true, false, false, false, false, false, true),
            ("BV10-016", "Bosvilla 10", "Bosrand", 10, 5, 2, 10, true, 2, false, true, true, true, true, true, true, false, false, false, true, true, false, false, false, false, false, true),
            ("BV10-017", "Bosvilla 10", "Rustweide", 10, 5, 3, 10, false, 0, false, false, true, true, true, false, true, false, true, true, true, true, true, false, false, false, false, true),
            ("BV10-018", "Bosvilla 10", "Rustweide", 10, 5, 2, 10, false, 0, false, true, true, true, true, true, true, false, true, true, true, true, true, false, false, false, false, true),
        ];

        // Pitches: code, type, zone, maxPersons, electricity, electricityAmps,
        // waterConnection, drainConnection, privateSanitary, carAtPitch, nearSanitary,
        // extraWide, surface, shade, wifiCoverage, maxCaravanLengthMeters,
        // maxVehicleLengthMeters, petsAllowed, maxPets.
        private static readonly (string Code, string Type, string Zone, int MaxPersons, bool Electricity, int Amps, bool Water, bool Drain, bool PrivateSanitary, bool CarAtPitch, bool NearSanitary, bool ExtraWide, string Surface, string Shade, string Wifi, double? MaxCaravanM, double? MaxVehicleM, bool Pets, int MaxPets)[] Pitches =
        [
            ("TB-001", "Tentplaats Basis", "Bosveld", 6, false, 0, false, false, false, false, false, false, "grass", "low", "limited", null, null, true, 2),
            ("TB-002", "Tentplaats Basis", "Bosveld", 5, false, 0, false, false, false, true, false, false, "grass", "medium", "limited", null, null, true, 2),
            ("TB-003", "Tentplaats Basis", "Waterweide", 5, true, 6, false, false, false, false, false, false, "grass", "high", "limited", null, null, true, 2),
            ("TB-004", "Tentplaats Basis", "Kampeerveld Noord", 4, true, 6, false, false, false, true, false, false, "grass", "medium", "standard", null, null, true, 2),
            ("TB-005", "Tentplaats Basis", "Kampeerveld Noord", 4, true, 10, false, false, false, true, true, false, "grass", "high", "standard", null, null, false, 0),
            ("TB-006", "Tentplaats Basis", "Waterweide", 6, false, 0, false, false, false, false, false, false, "grass", "high", "limited", null, null, true, 2),
            ("TB-007", "Tentplaats Basis", "Bosveld", 4, false, 0, false, false, false, false, true, false, "grass", "high", "standard", null, null, true, 2),
            ("TB-008", "Tentplaats Basis", "Waterweide", 6, true, 6, false, false, false, false, false, false, "grass", "high", "limited", null, null, true, 2),
            ("TB-009", "Tentplaats Basis", "Waterweide", 6, true, 6, false, false, false, false, true, false, "grass", "medium", "standard", null, null, true, 2),
            ("TB-010", "Tentplaats Basis", "Kampeerveld Zuid", 5, true, 10, false, false, false, true, false, false, "grass", "high", "standard", null, null, true, 2),
            ("TB-011", "Tentplaats Basis", "Kampeerveld Zuid", 4, false, 0, false, false, false, false, true, false, "grass", "low", "limited", null, null, true, 2),
            ("TB-012", "Tentplaats Basis", "Kampeerveld Noord", 4, false, 0, false, false, false, true, false, false, "grass", "low", "standard", null, null, true, 2),
            ("TC-001", "Tentplaats Comfort", "Kampeerveld Noord", 4, true, 16, true, false, false, true, true, false, "grass", "high", "good", null, null, false, 0),
            ("TC-002", "Tentplaats Comfort", "Kampeerveld Noord", 6, true, 10, true, false, false, false, true, false, "grass", "medium", "good", null, null, false, 0),
            ("TC-003", "Tentplaats Comfort", "Waterweide", 4, true, 16, false, false, true, true, false, false, "grass", "medium", "good", null, null, false, 0),
            ("TC-004", "Tentplaats Comfort", "Kampeerveld Noord", 4, true, 10, true, false, true, true, true, false, "grass", "medium", "standard", null, null, true, 2),
            ("TC-005", "Tentplaats Comfort", "Bosveld", 6, true, 10, false, false, true, true, false, false, "grass", "high", "good", null, null, false, 0),
            ("TC-006", "Tentplaats Comfort", "Kampeerveld Noord", 6, true, 10, true, false, false, true, false, false, "grass", "medium", "good", null, null, false, 0),
            ("TC-007", "Tentplaats Comfort", "Waterweide", 4, true, 10, true, false, false, false, true, false, "grass", "high", "standard", null, null, true, 2),
            ("TC-008", "Tentplaats Comfort", "Waterweide", 6, true, 10, true, false, false, true, false, false, "grass", "low", "good", null, null, false, 0),
            ("TC-009", "Tentplaats Comfort", "Bosveld", 6, true, 16, true, false, false, true, true, false, "grass", "low", "standard", null, null, true, 2),
            ("TC-010", "Tentplaats Comfort", "Waterweide", 6, true, 16, true, false, false, true, false, false, "grass", "medium", "standard", null, null, true, 2),
            ("CV-001", "Caravanplaats", "Bosveld", 6, true, 16, true, false, false, true, true, false, "reinforced_grass", "medium", "good", 8, null, true, 2),
            ("CV-002", "Caravanplaats", "Kampeerveld Zuid", 6, true, 10, true, true, true, true, true, false, "grass", "medium", "standard", 6.5, null, true, 2),
            ("CV-003", "Caravanplaats", "Kampeerveld Zuid", 6, true, 10, true, true, false, true, true, false, "reinforced_grass", "low", "standard", 8, null, true, 2),
            ("CV-004", "Caravanplaats", "Kampeerveld Noord", 6, true, 10, false, false, true, true, false, false, "grass", "medium", "good", 7, null, false, 0),
            ("CV-005", "Caravanplaats", "Kampeerveld Noord", 6, true, 16, true, false, false, true, true, false, "grass", "medium", "standard", 8, null, true, 2),
            ("CV-006", "Caravanplaats", "Kampeerveld Zuid", 6, true, 10, true, true, false, true, true, false, "grass", "medium", "standard", 8, null, true, 2),
            ("CV-007", "Caravanplaats", "Kampeerveld Zuid", 6, true, 10, false, false, false, true, true, false, "grass", "low", "standard", 7.5, null, true, 2),
            ("CV-008", "Caravanplaats", "Kampeerveld Noord", 6, true, 10, true, false, false, true, false, false, "grass", "medium", "good", 8, null, true, 2),
            ("CV-009", "Caravanplaats", "Bosveld", 6, true, 10, true, true, false, true, true, false, "reinforced_grass", "medium", "good", 6, null, false, 0),
            ("CV-010", "Caravanplaats", "Kampeerveld Noord", 6, true, 16, true, true, true, true, true, false, "grass", "medium", "standard", 6, null, true, 2),
            ("CV-011", "Caravanplaats", "Bosveld", 6, true, 10, true, true, false, true, true, false, "reinforced_grass", "low", "good", 8, null, true, 2),
            ("CV-012", "Caravanplaats", "Bosveld", 6, true, 16, true, true, false, true, true, false, "grass", "low", "good", 6.5, null, true, 2),
            ("CP-001", "Camperplaats", "Bosveld", 6, true, 10, true, true, false, true, false, false, "paved", "none", "standard", null, 7, true, 2),
            ("CP-002", "Camperplaats", "Kampeerveld Zuid", 6, true, 10, true, false, false, true, false, false, "reinforced_grass", "medium", "good", null, 7.5, false, 0),
            ("CP-003", "Camperplaats", "Kampeerveld Noord", 6, true, 16, true, true, false, true, false, false, "paved", "low", "good", null, 7.5, true, 2),
            ("CP-004", "Camperplaats", "Kampeerveld Zuid", 6, true, 10, true, true, false, true, true, false, "paved", "medium", "good", null, 8.5, true, 2),
            ("CP-005", "Camperplaats", "Kampeerveld Noord", 6, true, 16, true, true, false, true, false, false, "paved", "low", "standard", null, 8.5, true, 2),
            ("CP-006", "Camperplaats", "Kampeerveld Noord", 6, true, 10, true, true, false, true, true, false, "paved", "none", "standard", null, 7, true, 2),
            ("CP-007", "Camperplaats", "Bosveld", 6, true, 10, true, true, false, true, false, false, "reinforced_grass", "none", "good", null, 7, true, 2),
            ("CP-008", "Camperplaats", "Kampeerveld Noord", 6, true, 10, true, true, false, true, false, false, "paved", "medium", "good", null, 7, true, 2),
            ("CP-009", "Camperplaats", "Kampeerveld Noord", 6, true, 10, true, true, false, true, false, false, "paved", "low", "good", null, 8, false, 0),
            ("CP-010", "Camperplaats", "Kampeerveld Zuid", 6, true, 10, true, true, false, true, false, false, "paved", "low", "good", null, 8.5, true, 2),
            ("CPXL-001", "Camperplaats XL", "Bosveld", 6, true, 16, true, true, true, true, false, true, "paved", "none", "good", null, 11, false, 0),
            ("CPXL-002", "Camperplaats XL", "Kampeerveld Noord", 6, true, 16, true, true, false, true, true, true, "paved", "none", "good", null, 9, false, 0),
            ("CPXL-003", "Camperplaats XL", "Waterweide", 6, true, 16, true, true, false, true, true, true, "paved", "none", "good", null, 10, true, 2),
            ("CPXL-004", "Camperplaats XL", "Kampeerveld Noord", 6, true, 16, true, true, false, true, false, true, "paved", "none", "good", null, 9, true, 2),
            ("CPXL-005", "Camperplaats XL", "Kampeerveld Zuid", 6, true, 16, true, true, false, true, true, true, "paved", "low", "good", null, 10.5, true, 2),
            ("CPXL-006", "Camperplaats XL", "Kampeerveld Noord", 6, true, 16, true, true, false, true, true, true, "paved", "none", "good", null, 11, true, 2),
        ];

        public static List<AccommodationType> BuildTypes()
        {
            List<AccommodationType> types = [];
            foreach ((string slug, string name, string category, decimal[] _, decimal cleaningFee) in Types)
            {
                types.Add(new AccommodationType
                {
                    Id = Guid.NewGuid(),
                    Slug = slug,
                    Name = name,
                    Category = ParseCategory(category),
                    CleaningFee = cleaningFee,
                    CreatedAt = DateTime.UtcNow,
                });
            }
            return types;
        }

        public static List<Accommodation> BuildUnits()
        {
            List<Accommodation> units = [];
            foreach (var h in Homes)
            {
                units.Add(new Accommodation
                {
                    Id = Guid.NewGuid(),
                    Code = h.Code,
                    Name = h.Code,
                    Category = AccommodationCategory.HolidayHome,
                    Zone = h.Zone,
                    MaxPersons = h.MaxPersons,
                    PetsAllowed = h.Pets,
                    MaxPets = h.MaxPets,
                    WheelchairAccessible = h.Wheelchair,
                    Bedrooms = h.Bedrooms,
                    Bathrooms = h.Bathrooms,
                    Beds = h.Beds,
                    Sauna = h.Sauna,
                    Dishwasher = h.Dishwasher,
                    WashingMachine = h.WashingMachine,
                    AirConditioning = h.Airco,
                    Fireplace = h.Fireplace,
                    CoveredTerrace = h.CoveredTerrace,
                    ChargingPoint = h.ChargingPoint,
                    Waterfront = h.Waterfront,
                    NearPool = h.NearPool,
                    ChildFriendly = h.ChildFriendly,
                    SmokeFree = h.SmokeFree,
                    HotTub = h.HotTub,
                    LevelAccess = h.LevelAccess,
                    AdaptedBathroom = h.AdaptedBathroom,
                    WidenedDoors = h.WidenedDoors,
                    AdjustableBed = h.AdjustableBed,
                    ExtraParkingSpace = h.ExtraParking,
                    CreatedAt = DateTime.UtcNow,
                });
            }
            foreach (var p in Pitches)
            {
                units.Add(new Accommodation
                {
                    Id = Guid.NewGuid(),
                    Code = p.Code,
                    Name = p.Code,
                    Category = AccommodationCategory.CampingPitch,
                    Zone = p.Zone,
                    MaxPersons = p.MaxPersons,
                    Electricity = p.Electricity,
                    ElectricityAmps = p.Amps,
                    WaterConnection = p.Water,
                    DrainConnection = p.Drain,
                    PrivateSanitary = p.PrivateSanitary,
                    CarAtPitch = p.CarAtPitch,
                    NearSanitary = p.NearSanitary,
                    ExtraWide = p.ExtraWide,
                    Surface = MapSurface(p.Surface),
                    Shade = MapShade(p.Shade),
                    WifiCoverage = MapWifi(p.Wifi),
                    MaxCaravanLengthMeters = p.MaxCaravanM,
                    MaxVehicleLengthMeters = p.MaxVehicleM,
                    PetsAllowed = p.Pets,
                    MaxPets = p.MaxPets,
                    CreatedAt = DateTime.UtcNow,
                });
            }
            return units;
        }

        // Night rates per type name, for the US-05 price rows.
        public static IReadOnlyDictionary<string, decimal[]> NightRatesByType() =>
            Types.ToDictionary(t => t.Name, t => t.NightRates, StringComparer.OrdinalIgnoreCase);

        public static async Task SeedAccommodationsAsync(this ApplicationDBContext context)
        {
            List<Accommodation> existingUnits = await context.Accommodations
                .Where(unit => unit.DeletedAt == null)
                .ToListAsync();
            HashSet<string> knownCodes = existingUnits.Select(unit => unit.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);

            List<AccommodationType> existingTypeList = await context.AccommodationTypes
                .Where(type => type.DeletedAt == null)
                .ToListAsync();
            Dictionary<string, Guid> typeIds = existingTypeList.ToDictionary(type => type.Name, type => type.Id, StringComparer.OrdinalIgnoreCase);

            List<AccommodationType> newTypes = [];
            foreach ((string slug, string name, string category, decimal[] _, decimal cleaningFee) in Types)
            {
                if (typeIds.ContainsKey(name))
                    continue;

                AccommodationType type = new()
                {
                    Id = Guid.NewGuid(),
                    Slug = slug,
                    Name = name,
                    Category = ParseCategory(category),
                    CleaningFee = cleaningFee,
                    CreatedAt = DateTime.UtcNow,
                };
                typeIds[name] = type.Id;
                newTypes.Add(type);
            }
            context.AccommodationTypes.AddRange(newTypes);

            List<Accommodation> newUnits = [];
            foreach (Accommodation unit in BuildUnits())
            {
                if (knownCodes.Contains(unit.Code))
                    continue;

                unit.TypeId = TypeIdFor(unit.Code, typeIds);
                newUnits.Add(unit);
            }
            context.Accommodations.AddRange(newUnits);

            await context.SaveChangesAsync();
        }

        private static Guid TypeIdFor(string code, Dictionary<string, Guid> typeIds)
        {
            foreach (var h in Homes)
                if (h.Code == code)
                    return typeIds[h.Type];
            foreach (var p in Pitches)
                if (p.Code == code)
                    return typeIds[p.Type];

            throw new InvalidOperationException($"Voor seedeenheid '{code}' is geen type bekend.");
        }

        private static AccommodationCategory ParseCategory(string category) => category switch
        {
            "holiday_home" => AccommodationCategory.HolidayHome,
            "camping_pitch" => AccommodationCategory.CampingPitch,
            _ => throw new InvalidOperationException($"Onbekende categorie '{category}' in de seeddata."),
        };

        private static PitchSurface? MapSurface(string value) => value switch
        {
            "grass" => PitchSurface.Grass,
            "paved" => PitchSurface.Paved,
            "reinforced_grass" => PitchSurface.ReinforcedGrass,
            _ => null,
        };

        private static PitchShade? MapShade(string value) => value switch
        {
            "none" => PitchShade.None,
            "low" => PitchShade.Low,
            "medium" => PitchShade.Medium,
            "high" => PitchShade.High,
            _ => null,
        };

        private static WifiCoverage? MapWifi(string value) => value switch
        {
            "limited" => WifiCoverage.Limited,
            "standard" => WifiCoverage.Standard,
            "good" => WifiCoverage.Good,
            _ => null,
        };
    }
}
