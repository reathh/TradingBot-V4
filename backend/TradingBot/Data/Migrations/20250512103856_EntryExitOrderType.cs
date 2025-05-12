using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradingBot.Data.Migrations
{
    /// <inheritdoc />
    public partial class EntryExitOrderType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EntryOrderType",
                table: "Bots",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ExitOrderType",
                table: "Bots",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TradingMode",
                table: "Bots",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EntryOrderType",
                table: "Bots");

            migrationBuilder.DropColumn(
                name: "ExitOrderType",
                table: "Bots");

            migrationBuilder.DropColumn(
                name: "TradingMode",
                table: "Bots");
        }
    }
}
