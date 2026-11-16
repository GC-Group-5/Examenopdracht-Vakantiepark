using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vakantiepark.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddedBookingAccessToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "access_token",
                table: "bookings",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

			migrationBuilder.Sql("UPDATE bookings SET access_token = md5(random()::text || id::text) || md5(random()::text || id::text);");

			migrationBuilder.CreateIndex(
                name: "ix_bookings_access_token",
                table: "bookings",
                column: "access_token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_bookings_access_token",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "access_token",
                table: "bookings");
        }
    }
}
