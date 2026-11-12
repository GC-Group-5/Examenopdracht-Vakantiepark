using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vakantiepark.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddedBookingTravelersAndOverlapConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "birth_date",
                table: "bookings",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.CreateTable(
                name: "booking_travelers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    birth_date = table.Column<DateOnly>(type: "date", nullable: false),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_booking_travelers", x => x.id);
                    table.ForeignKey(
                        name: "fk_booking_travelers_bookings_booking_id",
                        column: x => x.booking_id,
                        principalTable: "bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_booking_travelers_booking_id",
                table: "booking_travelers",
                column: "booking_id");

            // Two bookings of the same unit may never overlap, even when they arrive at the same moment.
            // The departure day is exclusive ('[)'), so a guest may arrive on the day the previous guest leaves.
            // Deleted bookings do not block a unit. EF cannot model this constraint, so it is plain SQL.
            migrationBuilder.Sql("""
                ALTER TABLE bookings ADD CONSTRAINT ex_bookings_unit_no_overlap
                EXCLUDE USING gist (
                    accommodation_unit_id WITH =,
                    daterange(arrival_date, departure_date, '[)') WITH &&
                ) WHERE (deleted_at IS NULL);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE bookings DROP CONSTRAINT ex_bookings_unit_no_overlap;");

            migrationBuilder.DropTable(
                name: "booking_travelers");

            migrationBuilder.DropColumn(
                name: "birth_date",
                table: "bookings");
        }
    }
}
