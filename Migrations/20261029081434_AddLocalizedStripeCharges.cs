using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WellnessPlatform.Migrations
{
    /// <inheritdoc />
    public partial class AddLocalizedStripeCharges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ChargedAmountMinor",
                table: "Payments",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChargedCurrency",
                table: "Payments",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChargedAmountMinor",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ChargedCurrency",
                table: "Payments");
        }
    }
}
