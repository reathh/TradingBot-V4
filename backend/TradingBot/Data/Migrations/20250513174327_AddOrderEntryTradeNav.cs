using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradingBot.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderEntryTradeNav : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Trades_EntryOrderId",
                table: "Trades");

            migrationBuilder.CreateIndex(
                name: "IX_Trades_EntryOrderId",
                table: "Trades",
                column: "EntryOrderId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Trades_EntryOrderId",
                table: "Trades");

            migrationBuilder.CreateIndex(
                name: "IX_Trades_EntryOrderId",
                table: "Trades",
                column: "EntryOrderId");
        }
    }
}
