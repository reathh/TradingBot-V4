using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradingBot.Data.Migrations
{
    /// <inheritdoc />
    public partial class o3Indexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Trades_ExitOrderId",
                table: "Trades");

            migrationBuilder.CreateIndex(
                name: "IX_Trades_ExitOrderId",
                table: "Trades",
                column: "ExitOrderId",
                filter: "\"ExitOrderId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Filled_CreatedAt",
                table: "Orders",
                column: "CreatedAt",
                filter: "\"Status\" = 2")
                .Annotation("Npgsql:IndexInclude", new[] { "Price", "AverageFillPrice", "Quantity", "Fee" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Status_CreatedAt",
                table: "Orders",
                columns: new[] { "Status", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Trades_ExitOrderId",
                table: "Trades");

            migrationBuilder.DropIndex(
                name: "IX_Orders_Filled_CreatedAt",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_Status_CreatedAt",
                table: "Orders");

            migrationBuilder.CreateIndex(
                name: "IX_Trades_ExitOrderId",
                table: "Trades",
                column: "ExitOrderId");
        }
    }
}
