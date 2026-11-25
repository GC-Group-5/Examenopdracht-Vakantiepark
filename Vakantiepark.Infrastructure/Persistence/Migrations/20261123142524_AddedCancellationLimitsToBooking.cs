using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vakantiepark.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddedCancellationLimitsToBooking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "free_cancellation_days_before_arrival",
                table: "bookings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "no_refund_days_before_arrival",
                table: "bookings",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "free_cancellation_days_before_arrival",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "no_refund_days_before_arrival",
                table: "bookings");
        }
    }
}
