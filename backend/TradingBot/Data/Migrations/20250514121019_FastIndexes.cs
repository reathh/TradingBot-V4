using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradingBot.Data.Migrations
{
    /// <inheritdoc />
    public partial class FastIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Trades_BotId_ExitOrderId_EntryOrderId",
                table: "Trades",
                columns: new[] { "BotId", "ExitOrderId", "EntryOrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Status_Price",
                table: "Orders",
                columns: new[] { "Status", "Price" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Trades_BotId_ExitOrderId_EntryOrderId",
                table: "Trades");

            migrationBuilder.DropIndex(
                name: "IX_Orders_Status_Price",
                table: "Orders");
        }
    }
}
