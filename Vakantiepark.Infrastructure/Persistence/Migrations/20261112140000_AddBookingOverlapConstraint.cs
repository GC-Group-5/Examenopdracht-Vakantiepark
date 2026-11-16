using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vakantiepark.Infrastructure.Persistence.Migrations
{
    // Written by hand: EF cannot model an exclusion constraint. It makes PostgreSQL refuse a
    // second active booking of the same unit in an overlapping period, also when two requests
    // pass the availability check at the same moment (US-08). The departure day is excluded
    // ('[)'), so a new guest may arrive on the day the previous guest leaves.
    [DbContext(typeof(ApplicationDBContext))]
    [Migration("20261112140000_AddBookingOverlapConstraint")]
    public partial class AddBookingOverlapConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE bookings
                ADD CONSTRAINT ex_bookings_unit_period
                EXCLUDE USING gist (
                    accommodation_unit_id WITH =,
                    daterange(arrival_date, departure_date, '[)') WITH &&
                ) WHERE (deleted_at IS NULL);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE bookings DROP CONSTRAINT ex_bookings_unit_period;");
        }
    }
}
