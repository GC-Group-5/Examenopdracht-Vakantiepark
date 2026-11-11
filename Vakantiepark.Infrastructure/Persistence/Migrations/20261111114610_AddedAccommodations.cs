using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vakantiepark.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddedAccommodations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "accommodation_types",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    category = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_accommodation_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "accommodations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    name = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    category = table.Column<string>(type: "text", nullable: false),
                    zone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    max_persons = table.Column<int>(type: "integer", nullable: false),
                    pets_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    max_pets = table.Column<int>(type: "integer", nullable: false),
                    wheelchair_accessible = table.Column<bool>(type: "boolean", nullable: false),
                    bedrooms = table.Column<int>(type: "integer", nullable: false),
                    bathrooms = table.Column<int>(type: "integer", nullable: false),
                    beds = table.Column<int>(type: "integer", nullable: false),
                    sauna = table.Column<bool>(type: "boolean", nullable: false),
                    dishwasher = table.Column<bool>(type: "boolean", nullable: false),
                    washing_machine = table.Column<bool>(type: "boolean", nullable: false),
                    air_conditioning = table.Column<bool>(type: "boolean", nullable: false),
                    fireplace = table.Column<bool>(type: "boolean", nullable: false),
                    covered_terrace = table.Column<bool>(type: "boolean", nullable: false),
                    charging_point = table.Column<bool>(type: "boolean", nullable: false),
                    waterfront = table.Column<bool>(type: "boolean", nullable: false),
                    near_pool = table.Column<bool>(type: "boolean", nullable: false),
                    child_friendly = table.Column<bool>(type: "boolean", nullable: false),
                    smoke_free = table.Column<bool>(type: "boolean", nullable: false),
                    hot_tub = table.Column<bool>(type: "boolean", nullable: false),
                    level_access = table.Column<bool>(type: "boolean", nullable: false),
                    adapted_bathroom = table.Column<bool>(type: "boolean", nullable: false),
                    widened_doors = table.Column<bool>(type: "boolean", nullable: false),
                    adjustable_bed = table.Column<bool>(type: "boolean", nullable: false),
                    extra_parking_space = table.Column<bool>(type: "boolean", nullable: false),
                    electricity = table.Column<bool>(type: "boolean", nullable: false),
                    electricity_amps = table.Column<int>(type: "integer", nullable: false),
                    water_connection = table.Column<bool>(type: "boolean", nullable: false),
                    drain_connection = table.Column<bool>(type: "boolean", nullable: false),
                    private_sanitary = table.Column<bool>(type: "boolean", nullable: false),
                    car_at_pitch = table.Column<bool>(type: "boolean", nullable: false),
                    near_sanitary = table.Column<bool>(type: "boolean", nullable: false),
                    extra_wide = table.Column<bool>(type: "boolean", nullable: false),
                    surface = table.Column<string>(type: "text", nullable: true),
                    shade = table.Column<string>(type: "text", nullable: true),
                    wifi_coverage = table.Column<string>(type: "text", nullable: true),
                    max_caravan_length_meters = table.Column<double>(type: "double precision", nullable: true),
                    max_vehicle_length_meters = table.Column<double>(type: "double precision", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    type_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_accommodations", x => x.id);
                    table.ForeignKey(
                        name: "fk_accommodations_accommodation_types_type_id",
                        column: x => x.type_id,
                        principalTable: "accommodation_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_accommodation_types_name",
                table: "accommodation_types",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_accommodation_types_slug",
                table: "accommodation_types",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_accommodations_code",
                table: "accommodations",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_accommodations_type_id",
                table: "accommodations",
                column: "type_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "accommodations");

            migrationBuilder.DropTable(
                name: "accommodation_types");
        }
    }
}
