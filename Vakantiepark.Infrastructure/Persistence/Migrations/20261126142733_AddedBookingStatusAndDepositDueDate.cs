using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vakantiepark.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddedBookingStatusAndDepositDueDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "deposit_due_date",
                table: "bookings",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "bookings",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_bookings_status",
                table: "bookings",
                column: "status");

            // Existing bookings had no deposit tracking, so they count as paid; past stays as checked out.
            migrationBuilder.Sql(@"
                UPDATE bookings SET
                    status = CASE
                        WHEN deleted_at IS NOT NULL THEN 'Cancelled'
                        WHEN departure_date < CURRENT_DATE THEN 'CheckedOut'
                        ELSE 'Confirmed'
                    END,
                    deposit_due_date = arrival_date - COALESCE(free_cancellation_days_before_arrival, 30);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_bookings_status",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "deposit_due_date",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "status",
                table: "bookings");
        }
    }
}
