using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vakantiepark.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddedParkSettingsSeasonsAndPrices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "cleaning_fee",
                table: "accommodation_types",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

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
                name: "ix_accommodation_type_prices_accommodation_type_id_season_id",
                table: "accommodation_type_prices",
                columns: new[] { "accommodation_type_id", "season_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_accommodation_type_prices_season_id",
                table: "accommodation_type_prices",
                column: "season_id");

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
                name: "accommodation_type_prices");

            migrationBuilder.DropTable(
                name: "park_settings");

            migrationBuilder.DropTable(
                name: "seasons");

            migrationBuilder.DropColumn(
                name: "cleaning_fee",
                table: "accommodation_types");
        }
    }
}
