using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradingBot.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Trades_BotId_ExitOrderId",
                table: "Trades");

            migrationBuilder.DropIndex(
                name: "IX_Trades_BotId_ExitOrderId_EntryOrderId",
                table: "Trades");

            migrationBuilder.DropIndex(
                name: "IX_Orders_CreatedAt",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_Status",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_Status_CreatedAt",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_Status_Price",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Bots_Enabled_IsLong",
                table: "Bots");

            migrationBuilder.DropIndex(
                name: "IX_Bots_IsLong_MaxPrice_MinPrice",
                table: "Bots");

            migrationBuilder.CreateIndex(
                name: "IX_Trades_BotId",
                table: "Trades",
                column: "BotId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Trades_BotId",
                table: "Trades");

            migrationBuilder.CreateIndex(
                name: "IX_Trades_BotId_ExitOrderId",
                table: "Trades",
                columns: new[] { "BotId", "ExitOrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_Trades_BotId_ExitOrderId_EntryOrderId",
                table: "Trades",
                columns: new[] { "BotId", "ExitOrderId", "EntryOrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CreatedAt",
                table: "Orders",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Status",
                table: "Orders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Status_CreatedAt",
                table: "Orders",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Status_Price",
                table: "Orders",
                columns: new[] { "Status", "Price" });

            migrationBuilder.CreateIndex(
                name: "IX_Bots_Enabled_IsLong",
                table: "Bots",
                columns: new[] { "Enabled", "IsLong" });

            migrationBuilder.CreateIndex(
                name: "IX_Bots_IsLong_MaxPrice_MinPrice",
                table: "Bots",
                columns: new[] { "IsLong", "MaxPrice", "MinPrice" });
        }
    }
}
