using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradingBot.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAvailableCapital : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AvailableCapital",
                table: "Trades",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "AvailableCapital",
                table: "Bots",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvailableCapital",
                table: "Trades");

            migrationBuilder.DropColumn(
                name: "AvailableCapital",
                table: "Bots");
        }
    }
}
