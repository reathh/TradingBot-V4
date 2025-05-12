using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradingBot.Data.Migrations
{
    /// <inheritdoc />
    public partial class TradeEntryOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Trades_EntryTradeId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_EntryTradeId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "EntryTradeId",
                table: "Orders");

            migrationBuilder.AddColumn<string>(
                name: "EntryOrderId",
                table: "Trades",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Trades_EntryOrderId",
                table: "Trades",
                column: "EntryOrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_Trades_Orders_EntryOrderId",
                table: "Trades",
                column: "EntryOrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Trades_Orders_EntryOrderId",
                table: "Trades");

            migrationBuilder.DropIndex(
                name: "IX_Trades_EntryOrderId",
                table: "Trades");

            migrationBuilder.DropColumn(
                name: "EntryOrderId",
                table: "Trades");

            migrationBuilder.AddColumn<int>(
                name: "EntryTradeId",
                table: "Orders",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_EntryTradeId",
                table: "Orders",
                column: "EntryTradeId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Trades_EntryTradeId",
                table: "Orders",
                column: "EntryTradeId",
                principalTable: "Trades",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
