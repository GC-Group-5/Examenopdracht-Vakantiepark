using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vakantiepark.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddedSwimSlots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The park settings row already exists on every database; it gets the same pool defaults as the seeder.
            migrationBuilder.AddColumn<TimeOnly>(
                name: "pool_closes_at",
                table: "park_settings",
                type: "time without time zone",
                nullable: false,
                defaultValue: new TimeOnly(21, 0, 0));

            migrationBuilder.AddColumn<TimeOnly>(
                name: "pool_opens_at",
                table: "park_settings",
                type: "time without time zone",
                nullable: false,
                defaultValue: new TimeOnly(9, 0, 0));

            migrationBuilder.AddColumn<int>(
                name: "swim_slot_default_capacity",
                table: "park_settings",
                type: "integer",
                nullable: false,
                defaultValue: 80);

            migrationBuilder.AddColumn<int>(
                name: "swim_slot_minutes",
                table: "park_settings",
                type: "integer",
                nullable: false,
                defaultValue: 90);

            migrationBuilder.CreateTable(
                name: "swim_slots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    end_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    capacity = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_swim_slots", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_swim_slots_date_start_time",
                table: "swim_slots",
                columns: new[] { "date", "start_time" },
                unique: true,
                filter: "deleted_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "swim_slots");

            migrationBuilder.DropColumn(
                name: "pool_closes_at",
                table: "park_settings");

            migrationBuilder.DropColumn(
                name: "pool_opens_at",
                table: "park_settings");

            migrationBuilder.DropColumn(
                name: "swim_slot_default_capacity",
                table: "park_settings");

            migrationBuilder.DropColumn(
                name: "swim_slot_minutes",
                table: "park_settings");
        }
    }
}
