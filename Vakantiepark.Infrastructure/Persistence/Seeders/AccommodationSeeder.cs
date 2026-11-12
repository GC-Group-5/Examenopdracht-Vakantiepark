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
    //
    // The yes/no flags from the export are seeded as Feature rows linked through
    // the accommodation_features join table (direct many-to-many): a new feature
    // is one row in 'features', no code, no migration. Value columns (max persons,
    // max pets, bedrooms, amps, meters, surface, shade, wifi) stay real columns
    // because business rules use them. Category on the unit lives on its Type.
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

        // Feature table: key (machine key, kebab-case), Dutch name shown to
        // guests, category ("" = both). A new feature from the client = a new
        // row here, nothing else.
        private static readonly (string Key, string Name, string Category)[] Features =
        [
            ("sauna", "Sauna", "holiday_home"),
            ("dishwasher", "Dishwasmachine", "holiday_home"),
            ("washing-machine", "Wasmachine", "holiday_home"),
            ("airconditioning", "Airco", "holiday_home"),
            ("fireplace", "Open haard", "holiday_home"),
            ("covered-terrace", "Overdekt terras", "holiday_home"),
            ("charging-point", "Laadpaal", "holiday_home"),
            ("waterfront", "Waterkant", "holiday_home"),
            ("near-pool", "Bij zwembad", "holiday_home"),
            ("child-friendly", "Kindvriendelijk", "holiday_home"),
            ("smoke-free", "Rookvrij", "holiday_home"),
            ("hot-tub", "Hot tub", "holiday_home"),
            ("level-access", "Geen drempels", "holiday_home"),
            ("adapted-bathroom", "Aangepaste badkamer", "holiday_home"),
            ("widened-doors", "Verbrede deuren", "holiday_home"),
            ("adjustable-bed", "Verstelbaar bed", "holiday_home"),
            ("extra-parking-space", "Extra parkeerplaats", "holiday_home"),
            ("wheelchair-accessible", "Mindervalide", "holiday_home"),
            ("electricity", "Elektriciteit", "camping_pitch"),
            ("water-connection", "Wateraansluiting", "camping_pitch"),
            ("drain-connection", "Waterafvoer", "camping_pitch"),
            ("private-sanitary", "Eigen sanitair", "camping_pitch"),
            ("car-at-pitch", "Auto bij de plaats", "camping_pitch"),
            ("near-sanitary", "Bij sanitair", "camping_pitch"),
            ("extra-wide", "Extra brede plaats", "camping_pitch"),
            ("pets-allowed", "Honden toegestaan", ""),
        ];

        // Homes: code, type, zone, maxPersons, bedrooms, bathrooms, beds, maxPets,
        // feature keys. The flags feed FeatureKeysByUnit(); only value fields land
        // on the entity.
        private static readonly (string Code, string Type, string Zone, int MaxPersons, int Bedrooms, int Bathrooms, int Beds, int MaxPets, string[] Flags)[] Homes =
        [

            ("BK4-001", "Berk 4", "Rustweide", 4, 2, 1, 4, 2, ["dishwasher", "covered-terrace", "waterfront", "child-friendly", "smoke-free", "pets-allowed"]),
            ("BK4-002", "Berk 4", "Waterkant", 4, 2, 1, 4, 2, ["washing-machine", "airconditioning", "covered-terrace", "waterfront", "near-pool", "child-friendly", "smoke-free", "pets-allowed"]),
            ("BK4-003", "Berk 4", "Bosrand", 4, 2, 1, 4, 0, ["near-pool", "child-friendly", "smoke-free"]),
            ("BK4-004", "Berk 4", "Parkcentrum", 4, 2, 1, 4, 2, ["dishwasher", "covered-terrace", "near-pool", "smoke-free", "pets-allowed"]),
            ("BK4-005", "Berk 4", "Parkcentrum", 4, 2, 1, 4, 2, ["dishwasher", "covered-terrace", "smoke-free", "pets-allowed"]),
            ("BK4-006", "Berk 4", "Heideveld", 4, 2, 1, 4, 0, ["dishwasher", "washing-machine", "charging-point", "near-pool", "smoke-free"]),
            ("BK4-007", "Berk 4", "Parkcentrum", 4, 2, 1, 4, 0, ["covered-terrace", "smoke-free"]),
            ("BK4-008", "Berk 4", "Parkcentrum", 4, 2, 1, 4, 0, ["airconditioning", "fireplace", "covered-terrace", "waterfront", "smoke-free"]),
            ("BK4-009", "Berk 4", "Bosrand", 4, 2, 1, 4, 0, ["dishwasher", "covered-terrace", "near-pool", "child-friendly", "smoke-free"]),
            ("BK4-010", "Berk 4", "Waterkant", 4, 2, 1, 4, 2, ["dishwasher", "washing-machine", "covered-terrace", "child-friendly", "smoke-free", "pets-allowed"]),
            ("BK4-011", "Berk 4", "Rustweide", 4, 2, 1, 4, 0, ["dishwasher", "fireplace", "charging-point", "smoke-free"]),
            ("BK4-012", "Berk 4", "Heideveld", 4, 2, 1, 4, 2, ["dishwasher", "washing-machine", "waterfront", "child-friendly", "smoke-free", "pets-allowed"]),
            ("BK4-013", "Berk 4", "Waterkant", 4, 2, 1, 4, 2, ["charging-point", "waterfront", "child-friendly", "smoke-free", "pets-allowed"]),
            ("BK4-014", "Berk 4", "Parkcentrum", 4, 2, 1, 4, 2, ["dishwasher", "child-friendly", "smoke-free", "pets-allowed"]),
            ("BK4-015", "Berk 4", "Rustweide", 4, 2, 1, 4, 0, ["dishwasher", "washing-machine", "airconditioning", "covered-terrace", "near-pool", "smoke-free"]),
            ("BK4-016", "Berk 4", "Rustweide", 4, 2, 1, 4, 2, ["dishwasher", "airconditioning", "fireplace", "near-pool", "child-friendly", "smoke-free", "pets-allowed"]),
            ("BK4-017", "Berk 4", "Heideveld", 4, 2, 1, 4, 2, ["dishwasher", "washing-machine", "fireplace", "child-friendly", "smoke-free", "pets-allowed"]),
            ("BK4-018", "Berk 4", "Parkcentrum", 4, 2, 1, 4, 2, ["dishwasher", "washing-machine", "waterfront", "near-pool", "smoke-free", "pets-allowed"]),
            ("BK4-019", "Berk 4", "Waterkant", 4, 2, 1, 4, 0, ["dishwasher", "washing-machine", "charging-point", "waterfront", "child-friendly", "smoke-free"]),
            ("BK4-020", "Berk 4", "Bosrand", 4, 2, 1, 4, 0, ["dishwasher", "charging-point", "smoke-free"]),
            ("BK4-021", "Berk 4", "Rustweide", 4, 2, 1, 4, 2, ["dishwasher", "washing-machine", "fireplace", "child-friendly", "smoke-free", "pets-allowed"]),
            ("BK4-022", "Berk 4", "Heideveld", 4, 2, 1, 4, 2, ["dishwasher", "smoke-free", "pets-allowed"]),
            ("BK4-023", "Berk 4", "Parkcentrum", 4, 2, 1, 4, 2, ["dishwasher", "covered-terrace", "waterfront", "near-pool", "child-friendly", "smoke-free", "pets-allowed"]),
            ("BK4-024", "Berk 4", "Heideveld", 4, 2, 1, 4, 2, ["fireplace", "smoke-free", "pets-allowed"]),
            ("BK4-025", "Berk 4", "Parkcentrum", 4, 2, 1, 4, 0, ["airconditioning", "covered-terrace", "near-pool", "child-friendly", "smoke-free"]),
            ("BK4-026", "Berk 4", "Heideveld", 4, 2, 1, 4, 2, ["dishwasher", "fireplace", "near-pool", "child-friendly", "smoke-free", "wheelchair-accessible", "pets-allowed"]),
            ("BK4-027", "Berk 4", "Parkcentrum", 4, 2, 1, 4, 2, ["dishwasher", "fireplace", "child-friendly", "smoke-free", "pets-allowed"]),
            ("BK4-028", "Berk 4", "Rustweide", 4, 2, 1, 4, 2, ["dishwasher", "covered-terrace", "child-friendly", "smoke-free", "wheelchair-accessible", "pets-allowed"]),
            ("BK4-029", "Berk 4", "Heideveld", 4, 2, 1, 4, 2, ["dishwasher", "child-friendly", "smoke-free", "pets-allowed"]),
            ("BK4-030", "Berk 4", "Heideveld", 4, 2, 1, 4, 2, ["dishwasher", "airconditioning", "waterfront", "near-pool", "smoke-free", "pets-allowed"]),
            ("BK4-031", "Berk 4", "Waterkant", 4, 2, 1, 4, 0, ["washing-machine", "airconditioning", "charging-point", "near-pool", "child-friendly", "smoke-free"]),
            ("BK4-032", "Berk 4", "Parkcentrum", 4, 2, 1, 4, 0, ["covered-terrace", "waterfront", "child-friendly", "smoke-free"]),
            ("BK6-001", "Berk 6", "Rustweide", 6, 3, 1, 6, 2, ["dishwasher", "near-pool", "smoke-free", "pets-allowed"]),
            ("BK6-002", "Berk 6", "Heideveld", 6, 3, 1, 6, 0, ["dishwasher", "washing-machine", "fireplace", "covered-terrace", "near-pool", "child-friendly", "smoke-free"]),
            ("BK6-003", "Berk 6", "Heideveld", 6, 3, 1, 6, 0, ["dishwasher", "smoke-free"]),
            ("BK6-004", "Berk 6", "Parkcentrum", 6, 3, 1, 6, 0, ["dishwasher", "washing-machine", "covered-terrace", "near-pool", "child-friendly", "smoke-free"]),
            ("BK6-005", "Berk 6", "Heideveld", 6, 3, 1, 6, 0, ["dishwasher", "washing-machine", "covered-terrace", "child-friendly", "smoke-free"]),
            ("BK6-006", "Berk 6", "Rustweide", 6, 3, 1, 6, 0, ["dishwasher", "covered-terrace", "child-friendly", "smoke-free"]),
            ("BK6-007", "Berk 6", "Heideveld", 6, 3, 1, 6, 0, ["dishwasher", "washing-machine", "airconditioning", "child-friendly", "smoke-free"]),
            ("BK6-008", "Berk 6", "Bosrand", 6, 3, 1, 6, 0, ["dishwasher", "washing-machine", "airconditioning", "covered-terrace", "charging-point", "near-pool", "child-friendly", "smoke-free"]),
            ("BK6-009", "Berk 6", "Waterkant", 6, 3, 1, 6, 2, ["dishwasher", "washing-machine", "fireplace", "covered-terrace", "charging-point", "child-friendly", "smoke-free", "pets-allowed"]),
            ("BK6-010", "Berk 6", "Parkcentrum", 6, 3, 1, 6, 2, ["dishwasher", "covered-terrace", "child-friendly", "smoke-free", "pets-allowed"]),
            ("BK6-011", "Berk 6", "Waterkant", 6, 3, 1, 6, 0, ["dishwasher", "fireplace", "near-pool", "child-friendly", "smoke-free"]),
            ("BK6-012", "Berk 6", "Heideveld", 6, 3, 1, 6, 0, ["dishwasher", "washing-machine", "airconditioning", "child-friendly", "smoke-free"]),
            ("BK6-013", "Berk 6", "Heideveld", 6, 3, 1, 6, 0, ["dishwasher", "airconditioning", "fireplace", "waterfront", "near-pool", "child-friendly", "smoke-free"]),
            ("BK6-014", "Berk 6", "Bosrand", 6, 3, 1, 6, 0, ["dishwasher", "washing-machine", "child-friendly", "smoke-free"]),
            ("BK6-015", "Berk 6", "Heideveld", 6, 3, 1, 6, 0, ["dishwasher", "washing-machine", "fireplace", "child-friendly", "smoke-free"]),
            ("BK6-016", "Berk 6", "Parkcentrum", 6, 3, 1, 6, 2, ["dishwasher", "child-friendly", "smoke-free", "pets-allowed"]),
            ("BK6-017", "Berk 6", "Heideveld", 6, 3, 1, 6, 2, ["dishwasher", "washing-machine", "covered-terrace", "waterfront", "smoke-free", "pets-allowed"]),
            ("BK6-018", "Berk 6", "Rustweide", 6, 3, 1, 6, 0, ["dishwasher", "washing-machine", "fireplace", "charging-point", "child-friendly", "smoke-free"]),
            ("BK6-019", "Berk 6", "Parkcentrum", 6, 3, 1, 6, 2, ["dishwasher", "fireplace", "covered-terrace", "child-friendly", "smoke-free", "pets-allowed"]),
            ("BK6-020", "Berk 6", "Waterkant", 6, 3, 1, 6, 2, ["dishwasher", "washing-machine", "covered-terrace", "charging-point", "waterfront", "child-friendly", "smoke-free", "pets-allowed"]),
            ("BK6-021", "Berk 6", "Bosrand", 6, 3, 1, 6, 2, ["dishwasher", "washing-machine", "waterfront", "child-friendly", "smoke-free", "pets-allowed"]),
            ("BK6-022", "Berk 6", "Waterkant", 6, 3, 1, 6, 2, ["dishwasher", "washing-machine", "fireplace", "charging-point", "near-pool", "child-friendly", "smoke-free", "pets-allowed"]),
            ("BK6-023", "Berk 6", "Waterkant", 6, 3, 1, 6, 0, ["dishwasher", "washing-machine", "covered-terrace", "waterfront", "child-friendly", "smoke-free"]),
            ("BK6-024", "Berk 6", "Waterkant", 6, 3, 1, 6, 2, ["dishwasher", "covered-terrace", "charging-point", "waterfront", "near-pool", "smoke-free", "pets-allowed"]),
            ("BK6-025", "Berk 6", "Rustweide", 6, 3, 1, 6, 2, ["dishwasher", "washing-machine", "charging-point", "child-friendly", "smoke-free", "pets-allowed"]),
            ("BK6-026", "Berk 6", "Parkcentrum", 6, 3, 1, 6, 2, ["dishwasher", "covered-terrace", "waterfront", "child-friendly", "smoke-free", "pets-allowed"]),
            ("EK6-001", "Eik 6", "Rustweide", 6, 3, 1, 6, 0, ["dishwasher", "washing-machine", "covered-terrace", "near-pool", "child-friendly", "smoke-free"]),
            ("EK6-002", "Eik 6", "Heideveld", 6, 3, 2, 6, 0, ["dishwasher", "washing-machine", "airconditioning", "fireplace", "covered-terrace", "charging-point", "waterfront", "smoke-free"]),
            ("EK6-003", "Eik 6", "Bosrand", 6, 3, 2, 6, 2, ["dishwasher", "washing-machine", "airconditioning", "child-friendly", "smoke-free", "pets-allowed"]),
            ("EK6-004", "Eik 6", "Bosrand", 6, 3, 1, 6, 0, ["dishwasher", "fireplace", "near-pool", "child-friendly", "smoke-free"]),
            ("EK6-005", "Eik 6", "Rustweide", 6, 3, 2, 6, 0, ["dishwasher", "airconditioning", "fireplace", "smoke-free"]),
            ("EK6-006", "Eik 6", "Heideveld", 6, 3, 1, 6, 2, ["dishwasher", "washing-machine", "airconditioning", "fireplace", "covered-terrace", "smoke-free", "pets-allowed"]),
            ("EK6-007", "Eik 6", "Rustweide", 6, 3, 1, 6, 2, ["dishwasher", "washing-machine", "child-friendly", "smoke-free", "pets-allowed"]),
            ("EK6-008", "Eik 6", "Rustweide", 6, 3, 2, 6, 0, ["sauna", "dishwasher", "washing-machine", "airconditioning", "charging-point", "near-pool", "child-friendly", "smoke-free"]),
            ("EK6-009", "Eik 6", "Waterkant", 6, 3, 2, 6, 0, ["dishwasher", "washing-machine", "fireplace", "covered-terrace", "charging-point", "child-friendly", "smoke-free"]),
            ("EK6-010", "Eik 6", "Rustweide", 6, 3, 1, 6, 2, ["dishwasher", "washing-machine", "covered-terrace", "near-pool", "child-friendly", "smoke-free", "pets-allowed"]),
            ("EK6-011", "Eik 6", "Rustweide", 6, 3, 2, 6, 0, ["dishwasher", "washing-machine", "covered-terrace", "waterfront", "child-friendly", "smoke-free"]),
            ("EK6-012", "Eik 6", "Parkcentrum", 6, 3, 1, 6, 2, ["dishwasher", "airconditioning", "waterfront", "near-pool", "child-friendly", "smoke-free", "pets-allowed"]),
            ("EK6-013", "Eik 6", "Parkcentrum", 6, 3, 2, 6, 2, ["sauna", "dishwasher", "fireplace", "charging-point", "near-pool", "child-friendly", "smoke-free", "pets-allowed"]),
            ("EK6-014", "Eik 6", "Parkcentrum", 6, 3, 2, 6, 0, ["dishwasher", "charging-point", "near-pool", "child-friendly", "smoke-free"]),
            ("EK6-015", "Eik 6", "Rustweide", 6, 3, 2, 6, 2, ["sauna", "dishwasher", "washing-machine", "covered-terrace", "charging-point", "near-pool", "child-friendly", "smoke-free", "pets-allowed"]),
            ("EK6-016", "Eik 6", "Waterkant", 6, 3, 1, 6, 2, ["sauna", "dishwasher", "washing-machine", "near-pool", "child-friendly", "smoke-free", "pets-allowed"]),
            ("EK6-017", "Eik 6", "Waterkant", 6, 3, 1, 6, 2, ["sauna", "dishwasher", "airconditioning", "charging-point", "child-friendly", "smoke-free", "pets-allowed"]),
            ("EK6-018", "Eik 6", "Bosrand", 6, 3, 2, 6, 2, ["dishwasher", "washing-machine", "covered-terrace", "child-friendly", "smoke-free", "pets-allowed"]),
            ("EK6-019", "Eik 6", "Bosrand", 6, 3, 1, 6, 2, ["dishwasher", "washing-machine", "waterfront", "near-pool", "smoke-free", "pets-allowed"]),
            ("EK6-020", "Eik 6", "Heideveld", 6, 3, 1, 6, 2, ["sauna", "dishwasher", "fireplace", "covered-terrace", "charging-point", "smoke-free", "pets-allowed"]),
            ("EK6-021", "Eik 6", "Heideveld", 6, 3, 2, 6, 2, ["sauna", "dishwasher", "washing-machine", "charging-point", "near-pool", "child-friendly", "smoke-free", "pets-allowed"]),
            ("EK6-022", "Eik 6", "Rustweide", 6, 3, 2, 6, 0, ["dishwasher", "fireplace", "covered-terrace", "child-friendly", "smoke-free"]),
            ("EK6-023", "Eik 6", "Rustweide", 6, 3, 2, 6, 0, ["dishwasher", "covered-terrace", "near-pool", "smoke-free"]),
            ("EK6-024", "Eik 6", "Rustweide", 6, 3, 1, 6, 2, ["dishwasher", "washing-machine", "fireplace", "waterfront", "near-pool", "child-friendly", "smoke-free", "pets-allowed"]),
            ("EL6-001", "Eik Luxe 6", "Heideveld", 6, 3, 2, 6, 2, ["sauna", "dishwasher", "washing-machine", "covered-terrace", "charging-point", "child-friendly", "smoke-free", "pets-allowed"]),
            ("EL6-002", "Eik Luxe 6", "Rustweide", 6, 3, 2, 6, 2, ["dishwasher", "washing-machine", "fireplace", "covered-terrace", "waterfront", "near-pool", "smoke-free", "wheelchair-accessible", "pets-allowed"]),
            ("EL6-003", "Eik Luxe 6", "Waterkant", 6, 3, 2, 6, 0, ["sauna", "dishwasher", "washing-machine", "fireplace", "covered-terrace", "charging-point", "waterfront", "near-pool", "smoke-free", "hot-tub"]),
            ("EL6-004", "Eik Luxe 6", "Waterkant", 6, 3, 2, 6, 2, ["sauna", "dishwasher", "washing-machine", "airconditioning", "fireplace", "covered-terrace", "charging-point", "child-friendly", "smoke-free", "pets-allowed"]),
            ("EL6-005", "Eik Luxe 6", "Rustweide", 6, 3, 2, 6, 0, ["sauna", "dishwasher", "washing-machine", "airconditioning", "fireplace", "covered-terrace", "charging-point", "waterfront", "smoke-free"]),
            ("EL6-006", "Eik Luxe 6", "Waterkant", 6, 3, 2, 6, 2, ["sauna", "dishwasher", "washing-machine", "airconditioning", "fireplace", "covered-terrace", "charging-point", "near-pool", "smoke-free", "pets-allowed"]),
            ("EL6-007", "Eik Luxe 6", "Waterkant", 6, 3, 2, 6, 2, ["sauna", "dishwasher", "washing-machine", "airconditioning", "fireplace", "covered-terrace", "waterfront", "child-friendly", "smoke-free", "hot-tub", "pets-allowed"]),
            ("EL6-008", "Eik Luxe 6", "Parkcentrum", 6, 3, 2, 6, 0, ["sauna", "dishwasher", "washing-machine", "airconditioning", "fireplace", "covered-terrace", "charging-point", "near-pool", "child-friendly", "smoke-free", "hot-tub"]),
            ("EL6-009", "Eik Luxe 6", "Waterkant", 6, 3, 2, 6, 0, ["sauna", "dishwasher", "washing-machine", "airconditioning", "fireplace", "covered-terrace", "smoke-free"]),
            ("EL6-010", "Eik Luxe 6", "Rustweide", 6, 3, 2, 6, 0, ["sauna", "dishwasher", "washing-machine", "airconditioning", "covered-terrace", "child-friendly", "smoke-free"]),
            ("EL6-011", "Eik Luxe 6", "Heideveld", 6, 3, 2, 6, 2, ["sauna", "dishwasher", "washing-machine", "airconditioning", "covered-terrace", "charging-point", "waterfront", "child-friendly", "smoke-free", "pets-allowed"]),
            ("EL6-012", "Eik Luxe 6", "Rustweide", 6, 3, 2, 6, 2, ["sauna", "dishwasher", "washing-machine", "airconditioning", "covered-terrace", "charging-point", "child-friendly", "smoke-free", "hot-tub", "pets-allowed"]),
            ("EL6-013", "Eik Luxe 6", "Heideveld", 6, 3, 2, 6, 0, ["sauna", "dishwasher", "washing-machine", "airconditioning", "fireplace", "covered-terrace", "charging-point", "child-friendly", "smoke-free"]),
            ("EL6-014", "Eik Luxe 6", "Bosrand", 6, 3, 2, 6, 0, ["sauna", "dishwasher", "washing-machine", "airconditioning", "fireplace", "covered-terrace", "child-friendly", "smoke-free"]),
            ("EL6-015", "Eik Luxe 6", "Parkcentrum", 6, 3, 2, 6, 2, ["sauna", "dishwasher", "washing-machine", "airconditioning", "fireplace", "covered-terrace", "charging-point", "waterfront", "child-friendly", "smoke-free", "hot-tub", "pets-allowed"]),
            ("EL6-016", "Eik Luxe 6", "Rustweide", 6, 3, 2, 6, 0, ["sauna", "dishwasher", "washing-machine", "airconditioning", "fireplace", "covered-terrace", "charging-point", "near-pool", "child-friendly", "smoke-free"]),
            ("EL6-017", "Eik Luxe 6", "Parkcentrum", 6, 3, 2, 6, 0, ["dishwasher", "washing-machine", "airconditioning", "fireplace", "covered-terrace", "charging-point", "waterfront", "near-pool", "child-friendly", "smoke-free", "hot-tub"]),
            ("EL6-018", "Eik Luxe 6", "Bosrand", 6, 3, 2, 6, 0, ["sauna", "dishwasher", "washing-machine", "airconditioning", "fireplace", "covered-terrace", "waterfront", "near-pool", "child-friendly", "smoke-free", "hot-tub"]),
            ("BU8-001", "Beuk 8", "Parkcentrum", 8, 4, 3, 8, 0, ["sauna", "dishwasher", "washing-machine", "fireplace", "smoke-free"]),
            ("BU8-002", "Beuk 8", "Waterkant", 8, 4, 3, 8, 0, ["dishwasher", "washing-machine", "airconditioning", "waterfront", "child-friendly", "smoke-free"]),
            ("BU8-003", "Beuk 8", "Parkcentrum", 8, 4, 2, 8, 0, ["dishwasher", "airconditioning", "fireplace", "charging-point", "child-friendly", "smoke-free"]),
            ("BU8-004", "Beuk 8", "Parkcentrum", 8, 4, 2, 8, 0, ["dishwasher", "airconditioning", "fireplace", "covered-terrace", "charging-point", "child-friendly", "smoke-free"]),
            ("BU8-005", "Beuk 8", "Bosrand", 8, 4, 3, 8, 2, ["sauna", "dishwasher", "washing-machine", "fireplace", "covered-terrace", "waterfront", "near-pool", "smoke-free", "pets-allowed"]),
            ("BU8-006", "Beuk 8", "Bosrand", 8, 4, 2, 8, 0, ["dishwasher", "washing-machine", "child-friendly", "smoke-free", "wheelchair-accessible"]),
            ("BU8-007", "Beuk 8", "Heideveld", 8, 4, 3, 8, 2, ["dishwasher", "washing-machine", "covered-terrace", "waterfront", "near-pool", "child-friendly", "smoke-free", "pets-allowed"]),
            ("BU8-008", "Beuk 8", "Rustweide", 8, 4, 2, 8, 0, ["dishwasher", "washing-machine", "airconditioning", "fireplace", "covered-terrace", "charging-point", "child-friendly", "smoke-free"]),
            ("BU8-009", "Beuk 8", "Bosrand", 8, 4, 2, 8, 0, ["dishwasher", "washing-machine", "charging-point", "smoke-free"]),
            ("BU8-010", "Beuk 8", "Rustweide", 8, 4, 3, 8, 0, ["dishwasher", "washing-machine", "airconditioning", "fireplace", "covered-terrace", "near-pool", "child-friendly", "smoke-free"]),
            ("BU8-011", "Beuk 8", "Heideveld", 8, 4, 3, 8, 0, ["dishwasher", "washing-machine", "fireplace", "near-pool", "child-friendly", "smoke-free"]),
            ("BU8-012", "Beuk 8", "Heideveld", 8, 4, 2, 8, 2, ["dishwasher", "covered-terrace", "child-friendly", "smoke-free", "pets-allowed"]),
            ("BU8-013", "Beuk 8", "Heideveld", 8, 4, 2, 8, 0, ["dishwasher", "washing-machine", "airconditioning", "charging-point", "child-friendly", "smoke-free"]),
            ("BU8-014", "Beuk 8", "Heideveld", 8, 4, 3, 8, 0, ["dishwasher", "washing-machine", "airconditioning", "covered-terrace", "child-friendly", "smoke-free"]),
            ("BU8-015", "Beuk 8", "Bosrand", 8, 4, 2, 8, 2, ["dishwasher", "washing-machine", "airconditioning", "fireplace", "smoke-free", "pets-allowed"]),
            ("BU8-016", "Beuk 8", "Bosrand", 8, 4, 3, 8, 0, ["sauna", "dishwasher", "washing-machine", "airconditioning", "waterfront", "near-pool", "child-friendly", "smoke-free"]),
            ("BU8-017", "Beuk 8", "Waterkant", 8, 4, 3, 8, 0, ["sauna", "dishwasher", "washing-machine", "airconditioning", "covered-terrace", "child-friendly", "smoke-free"]),
            ("BU8-018", "Beuk 8", "Parkcentrum", 8, 4, 3, 8, 2, ["dishwasher", "washing-machine", "charging-point", "child-friendly", "smoke-free", "pets-allowed"]),
            ("BU8-019", "Beuk 8", "Heideveld", 8, 4, 2, 8, 2, ["dishwasher", "washing-machine", "fireplace", "covered-terrace", "waterfront", "child-friendly", "smoke-free", "pets-allowed"]),
            ("BU8-020", "Beuk 8", "Bosrand", 8, 4, 3, 8, 0, ["dishwasher", "washing-machine", "fireplace", "charging-point", "waterfront", "child-friendly", "smoke-free", "wheelchair-accessible"]),
            ("WG4M-001", "Wilg 4 Mindervalide", "Waterkant", 4, 2, 1, 4, 2, ["dishwasher", "washing-machine", "airconditioning", "child-friendly", "smoke-free", "level-access", "adapted-bathroom", "widened-doors", "adjustable-bed", "wheelchair-accessible", "pets-allowed"]),
            ("WG4M-002", "Wilg 4 Mindervalide", "Waterkant", 4, 2, 1, 4, 2, ["dishwasher", "washing-machine", "airconditioning", "covered-terrace", "near-pool", "child-friendly", "smoke-free", "level-access", "adapted-bathroom", "widened-doors", "adjustable-bed", "wheelchair-accessible", "pets-allowed"]),
            ("WG4M-003", "Wilg 4 Mindervalide", "Bosrand", 4, 2, 1, 4, 2, ["dishwasher", "covered-terrace", "charging-point", "near-pool", "child-friendly", "smoke-free", "level-access", "adapted-bathroom", "widened-doors", "wheelchair-accessible", "pets-allowed"]),
            ("WG4M-004", "Wilg 4 Mindervalide", "Rustweide", 4, 2, 1, 4, 0, ["dishwasher", "washing-machine", "airconditioning", "covered-terrace", "child-friendly", "smoke-free", "level-access", "adapted-bathroom", "widened-doors", "adjustable-bed", "wheelchair-accessible"]),
            ("WG4M-005", "Wilg 4 Mindervalide", "Bosrand", 4, 2, 1, 4, 0, ["dishwasher", "washing-machine", "near-pool", "child-friendly", "smoke-free", "level-access", "adapted-bathroom", "widened-doors", "wheelchair-accessible"]),
            ("WG4M-006", "Wilg 4 Mindervalide", "Rustweide", 4, 2, 1, 4, 0, ["dishwasher", "airconditioning", "covered-terrace", "near-pool", "child-friendly", "smoke-free", "level-access", "adapted-bathroom", "widened-doors", "adjustable-bed", "wheelchair-accessible"]),
            ("WG4M-007", "Wilg 4 Mindervalide", "Bosrand", 4, 2, 1, 4, 2, ["dishwasher", "washing-machine", "airconditioning", "covered-terrace", "near-pool", "child-friendly", "smoke-free", "level-access", "adapted-bathroom", "widened-doors", "adjustable-bed", "wheelchair-accessible", "pets-allowed"]),
            ("WG4M-008", "Wilg 4 Mindervalide", "Rustweide", 4, 2, 1, 4, 0, ["dishwasher", "covered-terrace", "child-friendly", "smoke-free", "level-access", "adapted-bathroom", "widened-doors", "adjustable-bed", "wheelchair-accessible"]),
            ("WG4M-009", "Wilg 4 Mindervalide", "Parkcentrum", 4, 2, 1, 4, 0, ["dishwasher", "washing-machine", "airconditioning", "covered-terrace", "waterfront", "near-pool", "child-friendly", "smoke-free", "level-access", "adapted-bathroom", "widened-doors", "adjustable-bed", "wheelchair-accessible"]),
            ("WG4M-010", "Wilg 4 Mindervalide", "Bosrand", 4, 2, 1, 4, 2, ["dishwasher", "covered-terrace", "child-friendly", "smoke-free", "level-access", "adapted-bathroom", "widened-doors", "adjustable-bed", "wheelchair-accessible", "pets-allowed"]),
            ("WG4M-011", "Wilg 4 Mindervalide", "Parkcentrum", 4, 2, 1, 4, 0, ["dishwasher", "airconditioning", "covered-terrace", "near-pool", "child-friendly", "smoke-free", "level-access", "adapted-bathroom", "widened-doors", "adjustable-bed", "wheelchair-accessible"]),
            ("WG4M-012", "Wilg 4 Mindervalide", "Rustweide", 4, 2, 1, 4, 0, ["dishwasher", "airconditioning", "covered-terrace", "charging-point", "near-pool", "child-friendly", "smoke-free", "level-access", "adapted-bathroom", "widened-doors", "adjustable-bed", "wheelchair-accessible"]),
            ("BV10-001", "Bosvilla 10", "Rustweide", 10, 5, 2, 10, 0, ["dishwasher", "washing-machine", "airconditioning", "fireplace", "covered-terrace", "waterfront", "near-pool", "child-friendly", "smoke-free"]),
            ("BV10-002", "Bosvilla 10", "Rustweide", 10, 5, 3, 10, 0, ["sauna", "dishwasher", "washing-machine", "airconditioning", "fireplace", "covered-terrace", "charging-point", "child-friendly", "smoke-free", "extra-parking-space"]),
            ("BV10-003", "Bosvilla 10", "Heideveld", 10, 5, 3, 10, 2, ["sauna", "dishwasher", "washing-machine", "fireplace", "covered-terrace", "waterfront", "child-friendly", "smoke-free", "pets-allowed"]),
            ("BV10-004", "Bosvilla 10", "Heideveld", 10, 5, 3, 10, 2, ["sauna", "dishwasher", "washing-machine", "fireplace", "covered-terrace", "charging-point", "child-friendly", "smoke-free", "pets-allowed"]),
            ("BV10-005", "Bosvilla 10", "Bosrand", 10, 5, 3, 10, 2, ["sauna", "dishwasher", "washing-machine", "airconditioning", "fireplace", "covered-terrace", "waterfront", "child-friendly", "smoke-free", "extra-parking-space", "pets-allowed"]),
            ("BV10-006", "Bosvilla 10", "Bosrand", 10, 5, 3, 10, 2, ["sauna", "dishwasher", "washing-machine", "fireplace", "covered-terrace", "charging-point", "waterfront", "child-friendly", "smoke-free", "extra-parking-space", "pets-allowed"]),
            ("BV10-007", "Bosvilla 10", "Rustweide", 10, 5, 2, 10, 2, ["sauna", "dishwasher", "washing-machine", "airconditioning", "fireplace", "charging-point", "waterfront", "child-friendly", "smoke-free", "extra-parking-space", "pets-allowed"]),
            ("BV10-008", "Bosvilla 10", "Waterkant", 10, 5, 2, 10, 2, ["sauna", "dishwasher", "washing-machine", "airconditioning", "covered-terrace", "child-friendly", "smoke-free", "extra-parking-space", "pets-allowed"]),
            ("BV10-009", "Bosvilla 10", "Rustweide", 10, 5, 2, 10, 2, ["sauna", "dishwasher", "washing-machine", "airconditioning", "fireplace", "covered-terrace", "charging-point", "near-pool", "child-friendly", "smoke-free", "extra-parking-space", "pets-allowed"]),
            ("BV10-010", "Bosvilla 10", "Rustweide", 10, 5, 2, 10, 2, ["dishwasher", "washing-machine", "airconditioning", "fireplace", "covered-terrace", "child-friendly", "smoke-free", "hot-tub", "extra-parking-space", "pets-allowed"]),
            ("BV10-011", "Bosvilla 10", "Bosrand", 10, 5, 3, 10, 2, ["sauna", "dishwasher", "washing-machine", "fireplace", "charging-point", "near-pool", "child-friendly", "smoke-free", "extra-parking-space", "pets-allowed"]),
            ("BV10-012", "Bosvilla 10", "Bosrand", 10, 5, 2, 10, 0, ["sauna", "dishwasher", "washing-machine", "airconditioning", "fireplace", "covered-terrace", "charging-point", "waterfront", "near-pool", "child-friendly", "smoke-free", "extra-parking-space"]),
            ("BV10-013", "Bosvilla 10", "Rustweide", 10, 5, 2, 10, 0, ["dishwasher", "washing-machine", "fireplace", "covered-terrace", "charging-point", "child-friendly", "smoke-free", "hot-tub", "extra-parking-space"]),
            ("BV10-014", "Bosvilla 10", "Parkcentrum", 10, 5, 2, 10, 2, ["sauna", "dishwasher", "washing-machine", "airconditioning", "fireplace", "covered-terrace", "child-friendly", "smoke-free", "pets-allowed"]),
            ("BV10-015", "Bosvilla 10", "Parkcentrum", 10, 5, 3, 10, 2, ["sauna", "dishwasher", "washing-machine", "airconditioning", "fireplace", "covered-terrace", "charging-point", "near-pool", "child-friendly", "smoke-free", "extra-parking-space", "pets-allowed"]),
            ("BV10-016", "Bosvilla 10", "Bosrand", 10, 5, 2, 10, 2, ["sauna", "dishwasher", "washing-machine", "airconditioning", "fireplace", "covered-terrace", "child-friendly", "smoke-free", "extra-parking-space", "pets-allowed"]),
            ("BV10-017", "Bosvilla 10", "Rustweide", 10, 5, 3, 10, 0, ["dishwasher", "washing-machine", "airconditioning", "covered-terrace", "waterfront", "near-pool", "child-friendly", "smoke-free", "hot-tub", "extra-parking-space"]),
            ("BV10-018", "Bosvilla 10", "Rustweide", 10, 5, 2, 10, 0, ["sauna", "dishwasher", "washing-machine", "airconditioning", "fireplace", "covered-terrace", "waterfront", "near-pool", "child-friendly", "smoke-free", "hot-tub", "extra-parking-space"]),
        ];

        // Pitches: code, type, zone, maxPersons, amps, surface, shade, wifi,
        // max caravan/vehicle meters, maxPets, feature keys.
        private static readonly (string Code, string Type, string Zone, int MaxPersons, int Amps, string? Surface, string? Shade, string? Wifi, double? MaxCaravanM, double? MaxVehicleM, int MaxPets, string[] Flags)[] Pitches =
        [

            ("TB-001", "Tentplaats Basis", "Bosveld", 6, 0, "grass", "low", "limited", null, null, 2, ["pets-allowed"]),
            ("TB-002", "Tentplaats Basis", "Bosveld", 5, 0, "grass", "medium", "limited", null, null, 2, ["car-at-pitch", "pets-allowed"]),
            ("TB-003", "Tentplaats Basis", "Waterweide", 5, 6, "grass", "high", "limited", null, null, 2, ["electricity", "pets-allowed"]),
            ("TB-004", "Tentplaats Basis", "Kampeerveld Noord", 4, 6, "grass", "medium", "standard", null, null, 2, ["electricity", "car-at-pitch", "pets-allowed"]),
            ("TB-005", "Tentplaats Basis", "Kampeerveld Noord", 4, 10, "grass", "high", "standard", null, null, 0, ["electricity", "car-at-pitch", "near-sanitary"]),
            ("TB-006", "Tentplaats Basis", "Waterweide", 6, 0, "grass", "high", "limited", null, null, 2, ["pets-allowed"]),
            ("TB-007", "Tentplaats Basis", "Bosveld", 4, 0, "grass", "high", "standard", null, null, 2, ["near-sanitary", "pets-allowed"]),
            ("TB-008", "Tentplaats Basis", "Waterweide", 6, 6, "grass", "high", "limited", null, null, 2, ["electricity", "pets-allowed"]),
            ("TB-009", "Tentplaats Basis", "Waterweide", 6, 6, "grass", "medium", "standard", null, null, 2, ["electricity", "near-sanitary", "pets-allowed"]),
            ("TB-010", "Tentplaats Basis", "Kampeerveld Zuid", 5, 10, "grass", "high", "standard", null, null, 2, ["electricity", "car-at-pitch", "pets-allowed"]),
            ("TB-011", "Tentplaats Basis", "Kampeerveld Zuid", 4, 0, "grass", "low", "limited", null, null, 2, ["near-sanitary", "pets-allowed"]),
            ("TB-012", "Tentplaats Basis", "Kampeerveld Noord", 4, 0, "grass", "low", "standard", null, null, 2, ["car-at-pitch", "pets-allowed"]),
            ("TC-001", "Tentplaats Comfort", "Kampeerveld Noord", 4, 16, "grass", "high", "good", null, null, 0, ["electricity", "water-connection", "car-at-pitch", "near-sanitary"]),
            ("TC-002", "Tentplaats Comfort", "Kampeerveld Noord", 6, 10, "grass", "medium", "good", null, null, 0, ["electricity", "water-connection", "near-sanitary"]),
            ("TC-003", "Tentplaats Comfort", "Waterweide", 4, 16, "grass", "medium", "good", null, null, 0, ["electricity", "private-sanitary", "car-at-pitch"]),
            ("TC-004", "Tentplaats Comfort", "Kampeerveld Noord", 4, 10, "grass", "medium", "standard", null, null, 2, ["electricity", "water-connection", "private-sanitary", "car-at-pitch", "near-sanitary", "pets-allowed"]),
            ("TC-005", "Tentplaats Comfort", "Bosveld", 6, 10, "grass", "high", "good", null, null, 0, ["electricity", "private-sanitary", "car-at-pitch"]),
            ("TC-006", "Tentplaats Comfort", "Kampeerveld Noord", 6, 10, "grass", "medium", "good", null, null, 0, ["electricity", "water-connection", "car-at-pitch"]),
            ("TC-007", "Tentplaats Comfort", "Waterweide", 4, 10, "grass", "high", "standard", null, null, 2, ["electricity", "water-connection", "near-sanitary", "pets-allowed"]),
            ("TC-008", "Tentplaats Comfort", "Waterweide", 6, 10, "grass", "low", "good", null, null, 0, ["electricity", "water-connection", "car-at-pitch"]),
            ("TC-009", "Tentplaats Comfort", "Bosveld", 6, 16, "grass", "low", "standard", null, null, 2, ["electricity", "water-connection", "car-at-pitch", "near-sanitary", "pets-allowed"]),
            ("TC-010", "Tentplaats Comfort", "Waterweide", 6, 16, "grass", "medium", "standard", null, null, 2, ["electricity", "water-connection", "car-at-pitch", "pets-allowed"]),
            ("CV-001", "Caravanplaats", "Bosveld", 6, 16, "reinforced_grass", "medium", "good", 8, null, 2, ["electricity", "water-connection", "car-at-pitch", "near-sanitary", "pets-allowed"]),
            ("CV-002", "Caravanplaats", "Kampeerveld Zuid", 6, 10, "grass", "medium", "standard", 6.5, null, 2, ["electricity", "water-connection", "drain-connection", "private-sanitary", "car-at-pitch", "near-sanitary", "pets-allowed"]),
            ("CV-003", "Caravanplaats", "Kampeerveld Zuid", 6, 10, "reinforced_grass", "low", "standard", 8, null, 2, ["electricity", "water-connection", "drain-connection", "car-at-pitch", "near-sanitary", "pets-allowed"]),
            ("CV-004", "Caravanplaats", "Kampeerveld Noord", 6, 10, "grass", "medium", "good", 7, null, 0, ["electricity", "private-sanitary", "car-at-pitch"]),
            ("CV-005", "Caravanplaats", "Kampeerveld Noord", 6, 16, "grass", "medium", "standard", 8, null, 2, ["electricity", "water-connection", "car-at-pitch", "near-sanitary", "pets-allowed"]),
            ("CV-006", "Caravanplaats", "Kampeerveld Zuid", 6, 10, "grass", "medium", "standard", 8, null, 2, ["electricity", "water-connection", "drain-connection", "car-at-pitch", "near-sanitary", "pets-allowed"]),
            ("CV-007", "Caravanplaats", "Kampeerveld Zuid", 6, 10, "grass", "low", "standard", 7.5, null, 2, ["electricity", "car-at-pitch", "near-sanitary", "pets-allowed"]),
            ("CV-008", "Caravanplaats", "Kampeerveld Noord", 6, 10, "grass", "medium", "good", 8, null, 2, ["electricity", "water-connection", "car-at-pitch", "pets-allowed"]),
            ("CV-009", "Caravanplaats", "Bosveld", 6, 10, "reinforced_grass", "medium", "good", 6, null, 0, ["electricity", "water-connection", "drain-connection", "car-at-pitch", "near-sanitary"]),
            ("CV-010", "Caravanplaats", "Kampeerveld Noord", 6, 16, "grass", "medium", "standard", 6, null, 2, ["electricity", "water-connection", "drain-connection", "private-sanitary", "car-at-pitch", "near-sanitary", "pets-allowed"]),
            ("CV-011", "Caravanplaats", "Bosveld", 6, 10, "reinforced_grass", "low", "good", 8, null, 2, ["electricity", "water-connection", "drain-connection", "car-at-pitch", "near-sanitary", "pets-allowed"]),
            ("CV-012", "Caravanplaats", "Bosveld", 6, 16, "grass", "low", "good", 6.5, null, 2, ["electricity", "water-connection", "drain-connection", "car-at-pitch", "near-sanitary", "pets-allowed"]),
            ("CP-001", "Camperplaats", "Bosveld", 6, 10, "paved", "none", "standard", null, 7, 2, ["electricity", "water-connection", "drain-connection", "car-at-pitch", "pets-allowed"]),
            ("CP-002", "Camperplaats", "Kampeerveld Zuid", 6, 10, "reinforced_grass", "medium", "good", null, 7.5, 0, ["electricity", "water-connection", "car-at-pitch"]),
            ("CP-003", "Camperplaats", "Kampeerveld Noord", 6, 16, "paved", "low", "good", null, 7.5, 2, ["electricity", "water-connection", "drain-connection", "car-at-pitch", "pets-allowed"]),
            ("CP-004", "Camperplaats", "Kampeerveld Zuid", 6, 10, "paved", "medium", "good", null, 8.5, 2, ["electricity", "water-connection", "drain-connection", "car-at-pitch", "near-sanitary", "pets-allowed"]),
            ("CP-005", "Camperplaats", "Kampeerveld Noord", 6, 16, "paved", "low", "standard", null, 8.5, 2, ["electricity", "water-connection", "drain-connection", "car-at-pitch", "pets-allowed"]),
            ("CP-006", "Camperplaats", "Kampeerveld Noord", 6, 10, "paved", "none", "standard", null, 7, 2, ["electricity", "water-connection", "drain-connection", "car-at-pitch", "near-sanitary", "pets-allowed"]),
            ("CP-007", "Camperplaats", "Bosveld", 6, 10, "reinforced_grass", "none", "good", null, 7, 2, ["electricity", "water-connection", "drain-connection", "car-at-pitch", "pets-allowed"]),
            ("CP-008", "Camperplaats", "Kampeerveld Noord", 6, 10, "paved", "medium", "good", null, 7, 2, ["electricity", "water-connection", "drain-connection", "car-at-pitch", "pets-allowed"]),
            ("CP-009", "Camperplaats", "Kampeerveld Noord", 6, 10, "paved", "low", "good", null, 8, 0, ["electricity", "water-connection", "drain-connection", "car-at-pitch"]),
            ("CP-010", "Camperplaats", "Kampeerveld Zuid", 6, 10, "paved", "low", "good", null, 8.5, 2, ["electricity", "water-connection", "drain-connection", "car-at-pitch", "pets-allowed"]),
            ("CPXL-001", "Camperplaats XL", "Bosveld", 6, 16, "paved", "none", "good", null, 11, 0, ["electricity", "water-connection", "drain-connection", "private-sanitary", "car-at-pitch", "extra-wide"]),
            ("CPXL-002", "Camperplaats XL", "Kampeerveld Noord", 6, 16, "paved", "none", "good", null, 9, 0, ["electricity", "water-connection", "drain-connection", "car-at-pitch", "near-sanitary", "extra-wide"]),
            ("CPXL-003", "Camperplaats XL", "Waterweide", 6, 16, "paved", "none", "good", null, 10, 2, ["electricity", "water-connection", "drain-connection", "car-at-pitch", "near-sanitary", "extra-wide", "pets-allowed"]),
            ("CPXL-004", "Camperplaats XL", "Kampeerveld Noord", 6, 16, "paved", "none", "good", null, 9, 2, ["electricity", "water-connection", "drain-connection", "car-at-pitch", "extra-wide", "pets-allowed"]),
            ("CPXL-005", "Camperplaats XL", "Kampeerveld Zuid", 6, 16, "paved", "low", "good", null, 10.5, 2, ["electricity", "water-connection", "drain-connection", "car-at-pitch", "near-sanitary", "extra-wide", "pets-allowed"]),
            ("CPXL-006", "Camperplaats XL", "Kampeerveld Noord", 6, 16, "paved", "none", "good", null, 11, 2, ["electricity", "water-connection", "drain-connection", "car-at-pitch", "near-sanitary", "extra-wide", "pets-allowed"]),
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

        public static List<Feature> BuildFeatures()
        {
            List<Feature> features = [];
            foreach ((string key, string name, string category) in Features)
            {
                features.Add(new Feature
                {
                    Id = Guid.NewGuid(),
                    Key = key,
                    Name = name,
                    Category = ParseFeatureCategory(category),
                    CreatedAt = DateTime.UtcNow,
                });
            }
            return features;
        }

        // Unit code -> feature keys, from the export flags.
        public static IReadOnlyDictionary<string, string[]> FeatureKeysByUnit()
        {
            Dictionary<string, string[]> keys = new(StringComparer.OrdinalIgnoreCase);
            foreach (var h in Homes)
                keys[h.Code] = h.Flags;
            foreach (var p in Pitches)
                keys[p.Code] = p.Flags;
            return keys;
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
                    Zone = h.Zone,
                    MaxPersons = h.MaxPersons,
                    MaxPets = h.MaxPets,
                    Bedrooms = h.Bedrooms,
                    Bathrooms = h.Bathrooms,
                    Beds = h.Beds,
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
                    Zone = p.Zone,
                    MaxPersons = p.MaxPersons,
                    MaxPets = p.MaxPets,
                    ElectricityAmps = p.Amps,
                    Surface = MapSurface(p.Surface),
                    Shade = MapShade(p.Shade),
                    WifiCoverage = MapWifi(p.Wifi),
                    MaxCaravanLengthMeters = p.MaxCaravanM,
                    MaxVehicleLengthMeters = p.MaxVehicleM,
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
            // Features first: the link rows reference them, and a feature row
            // alone is a valid state (a feature nobody has yet = an empty chip list).
            List<Feature> trackedFeatures = await context.Features.ToListAsync();
            Dictionary<string, Feature> featuresByKey = trackedFeatures
                .ToDictionary(feature => feature.Key, StringComparer.OrdinalIgnoreCase);
            List<Feature> newFeatures = BuildFeatures()
                .Where(feature => !featuresByKey.ContainsKey(feature.Key))
                .ToList();
            foreach (Feature feature in newFeatures)
                featuresByKey[feature.Key] = feature;
            context.Features.AddRange(newFeatures);

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

            // Units only when their code is new (idempotent re-run); existing
            // units keep their features untouched.
            HashSet<string> knownCodes = (await context.Accommodations
                .Where(unit => unit.DeletedAt == null)
                .ToListAsync())
                .Select(unit => unit.Code)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            IReadOnlyDictionary<string, string[]> keysByUnit = FeatureKeysByUnit();
            List<Accommodation> newUnits = [];
            foreach (Accommodation unit in BuildUnits())
            {
                if (knownCodes.Contains(unit.Code))
                    continue;

                unit.TypeId = TypeIdFor(unit.Code, typeIds);
                string[] keys = keysByUnit.TryGetValue(unit.Code, out string[]? unitKeys) ? unitKeys : [];
                unit.Features = [.. keys.Select(key => featuresByKey[key])];
                newUnits.Add(unit);
            }
            context.Accommodations.AddRange(newUnits);

            // EF inserts the accommodation_features rows from the navigations.
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

        // "" (empty) = the feature applies to both categories (null column).
        private static AccommodationCategory? ParseFeatureCategory(string category) => category switch
        {
            "" => null,
            "holiday_home" => AccommodationCategory.HolidayHome,
            "camping_pitch" => AccommodationCategory.CampingPitch,
            _ => throw new InvalidOperationException($"Onbekende feature-categorie '{category}' in de seeddata."),
        };

        private static PitchSurface? MapSurface(string? value) => value switch
        {
            "grass" => PitchSurface.Grass,
            "paved" => PitchSurface.Paved,
            "reinforced_grass" => PitchSurface.ReinforcedGrass,
            _ => null,
        };

        private static PitchShade? MapShade(string? value) => value switch
        {
            "none" => PitchShade.None,
            "low" => PitchShade.Low,
            "medium" => PitchShade.Medium,
            "high" => PitchShade.High,
            _ => null,
        };

        private static WifiCoverage? MapWifi(string? value) => value switch
        {
            "limited" => WifiCoverage.Limited,
            "standard" => WifiCoverage.Standard,
            "good" => WifiCoverage.Good,
            _ => null,
        };
    }
}