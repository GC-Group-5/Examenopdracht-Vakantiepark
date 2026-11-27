using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vakantiepark.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddedBankAccountToParkSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "bank_account_name",
                table: "park_settings",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "bank_account_number",
                table: "park_settings",
                type: "character varying(34)",
                maxLength: 34,
                nullable: false,
                defaultValue: "");

            // The park settings row already exists; it gets the test account until the client gives the real one.
            migrationBuilder.Sql(@"
                UPDATE park_settings
                SET bank_account_number = 'NL91 ABNA 0417 1643 00',
                    bank_account_name = 'Vakantiepark D''r op Uut'
                WHERE bank_account_number = '';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "bank_account_name",
                table: "park_settings");

            migrationBuilder.DropColumn(
                name: "bank_account_number",
                table: "park_settings");
        }
    }
}
