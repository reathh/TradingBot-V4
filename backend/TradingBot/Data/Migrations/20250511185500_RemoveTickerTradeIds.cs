using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradingBot.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTickerTradeIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstTradeId",
                table: "Tickers");

            migrationBuilder.DropColumn(
                name: "LastTradeId",
                table: "Tickers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "FirstTradeId",
                table: "Tickers",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "LastTradeId",
                table: "Tickers",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }
    }
}
