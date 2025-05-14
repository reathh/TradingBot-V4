using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradingBot.Data.Migrations
{
    /// <inheritdoc />
    public partial class OrderAndTradeIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_CreatedAt_Status",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_Status_QuantityFilled",
                table: "Orders");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Status_CreatedAt",
                table: "Orders",
                columns: new[] { "Status", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_Status_CreatedAt",
                table: "Orders");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CreatedAt_Status",
                table: "Orders",
                columns: new[] { "CreatedAt", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Status_QuantityFilled",
                table: "Orders",
                columns: new[] { "Status", "QuantityFilled" });
        }
    }
}
