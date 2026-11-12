using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vakantiepark.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddedParkSettingsSeasonsPricesAndFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "adapted_bathroom",
                table: "accommodations");

            migrationBuilder.DropColumn(
                name: "adjustable_bed",
                table: "accommodations");

            migrationBuilder.DropColumn(
                name: "air_conditioning",
                table: "accommodations");

            migrationBuilder.DropColumn(
                name: "car_at_pitch",
                table: "accommodations");

            migrationBuilder.DropColumn(
                name: "category",
                table: "accommodations");

            migrationBuilder.DropColumn(
                name: "charging_point",
                table: "accommodations");

            migrationBuilder.DropColumn(
                name: "child_friendly",
                table: "accommodations");

            migrationBuilder.DropColumn(
                name: "covered_terrace",
                table: "accommodations");

            migrationBuilder.DropColumn(
                name: "dishwasher",
                table: "accommodations");

            migrationBuilder.DropColumn(
                name: "drain_connection",
                table: "accommodations");

            migrationBuilder.DropColumn(
                name: "electricity",
                table: "accommodations");

            migrationBuilder.DropColumn(
                name: "extra_parking_space",
                table: "accommodations");

            migrationBuilder.DropColumn(
                name: "extra_wide",
                table: "accommodations");

            migrationBuilder.DropColumn(
                name: "fireplace",
                table: "accommodations");

            migrationBuilder.DropColumn(
                name: "hot_tub",
                table: "accommodations");

            migrationBuilder.DropColumn(
                name: "level_access",
                table: "accommodations");

            migrationBuilder.DropColumn(
                name: "near_pool",
                table: "accommodations");

            migrationBuilder.DropColumn(
                name: "near_sanitary",
                table: "accommodations");

            migrationBuilder.DropColumn(
                name: "pets_allowed",
                table: "accommodations");

            migrationBuilder.DropColumn(
                name: "private_sanitary",
                table: "accommodations");

            migrationBuilder.DropColumn(
                name: "sauna",
                table: "accommodations");

            migrationBuilder.DropColumn(
                name: "smoke_free",
                table: "accommodations");

            migrationBuilder.DropColumn(
                name: "washing_machine",
                table: "accommodations");

            migrationBuilder.DropColumn(
                name: "water_connection",
                table: "accommodations");

            migrationBuilder.DropColumn(
                name: "waterfront",
                table: "accommodations");

            migrationBuilder.DropColumn(
                name: "wheelchair_accessible",
                table: "accommodations");

            migrationBuilder.DropColumn(
                name: "widened_doors",
                table: "accommodations");

            migrationBuilder.AlterColumn<int>(
                name: "electricity_amps",
                table: "accommodations",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "beds",
                table: "accommodations",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "bedrooms",
                table: "accommodations",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "bathrooms",
                table: "accommodations",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<decimal>(
                name: "cleaning_fee",
                table: "accommodation_types",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "features",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    category = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_features", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "park_settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    arrival_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    departure_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    tourist_tax_per_person_per_night = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    high_season_minimum_nights = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_park_settings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "seasons",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_seasons", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "accommodation_features",
                columns: table => new
                {
                    accommodations_id = table.Column<Guid>(type: "uuid", nullable: false),
                    features_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_accommodation_features", x => new { x.accommodations_id, x.features_id });
                    table.ForeignKey(
                        name: "fk_accommodation_features_accommodations_accommodations_id",
                        column: x => x.accommodations_id,
                        principalTable: "accommodations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_accommodation_features_features_features_id",
                        column: x => x.features_id,
                        principalTable: "features",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "accommodation_type_prices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    price_per_night = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    accommodation_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    season_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_accommodation_type_prices", x => x.id);
                    table.ForeignKey(
                        name: "fk_accommodation_type_prices_accommodation_types_accommodation",
                        column: x => x.accommodation_type_id,
                        principalTable: "accommodation_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_accommodation_type_prices_seasons_season_id",
                        column: x => x.season_id,
                        principalTable: "seasons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_accommodation_features_features_id",
                table: "accommodation_features",
                column: "features_id");

            migrationBuilder.CreateIndex(
                name: "ix_accommodation_type_prices_accommodation_type_id_season_id",
                table: "accommodation_type_prices",
                columns: new[] { "accommodation_type_id", "season_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_accommodation_type_prices_season_id",
                table: "accommodation_type_prices",
                column: "season_id");

            migrationBuilder.CreateIndex(
                name: "ix_features_key",
                table: "features",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_seasons_name",
                table: "seasons",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_seasons_start_date_end_date",
                table: "seasons",
                columns: new[] { "start_date", "end_date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "accommodation_features");

            migrationBuilder.DropTable(
                name: "accommodation_type_prices");

            migrationBuilder.DropTable(
                name: "park_settings");

            migrationBuilder.DropTable(
                name: "features");

            migrationBuilder.DropTable(
                name: "seasons");

            migrationBuilder.DropColumn(
                name: "cleaning_fee",
                table: "accommodation_types");

            migrationBuilder.AlterColumn<int>(
                name: "electricity_amps",
                table: "accommodations",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "beds",
                table: "accommodations",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "bedrooms",
                table: "accommodations",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "bathrooms",
                table: "accommodations",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "adapted_bathroom",
                table: "accommodations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "adjustable_bed",
                table: "accommodations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "air_conditioning",
                table: "accommodations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "car_at_pitch",
                table: "accommodations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "category",
                table: "accommodations",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "charging_point",
                table: "accommodations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "child_friendly",
                table: "accommodations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "covered_terrace",
                table: "accommodations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "dishwasher",
                table: "accommodations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "drain_connection",
                table: "accommodations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "electricity",
                table: "accommodations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "extra_parking_space",
                table: "accommodations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "extra_wide",
                table: "accommodations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "fireplace",
                table: "accommodations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "hot_tub",
                table: "accommodations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "level_access",
                table: "accommodations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "near_pool",
                table: "accommodations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "near_sanitary",
                table: "accommodations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "pets_allowed",
                table: "accommodations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "private_sanitary",
                table: "accommodations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "sauna",
                table: "accommodations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "smoke_free",
                table: "accommodations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "washing_machine",
                table: "accommodations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "water_connection",
                table: "accommodations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "waterfront",
                table: "accommodations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "wheelchair_accessible",
                table: "accommodations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "widened_doors",
                table: "accommodations",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
