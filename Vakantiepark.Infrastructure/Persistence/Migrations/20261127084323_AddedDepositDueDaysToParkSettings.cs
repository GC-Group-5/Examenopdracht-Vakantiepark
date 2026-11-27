using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vakantiepark.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddedDepositDueDaysToParkSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "deposit_due_days_before_arrival",
                table: "park_settings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // The park starts with 7 days; unpaid bookings get their due date by the new rule.
            migrationBuilder.Sql(@"
                UPDATE park_settings SET deposit_due_days_before_arrival = 7;

                -- For a database that already ran the previous migration without the test account.
                UPDATE park_settings
                SET bank_account_number = 'NL91 ABNA 0417 1643 00',
                    bank_account_name = 'Vakantiepark D''r op Uut'
                WHERE bank_account_number = '';

                UPDATE bookings SET deposit_due_date = CASE
                        WHEN arrival_date - COALESCE(free_cancellation_days_before_arrival, 30) <= created_at::date THEN created_at::date
                        ELSE arrival_date - 7
                    END
                WHERE status = 'AwaitingDeposit';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "deposit_due_days_before_arrival",
                table: "park_settings");
        }
    }
}
