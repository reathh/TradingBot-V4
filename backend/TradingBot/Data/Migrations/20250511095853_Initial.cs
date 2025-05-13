using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TradingBot.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Bots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    PublicKey = table.Column<string>(type: "text", nullable: false),
                    PrivateKey = table.Column<string>(type: "text", nullable: false),
                    Symbol = table.Column<string>(type: "text", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    MaxPrice = table.Column<decimal>(type: "numeric", nullable: true),
                    MinPrice = table.Column<decimal>(type: "numeric", nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    PlaceOrdersInAdvance = table.Column<bool>(type: "boolean", nullable: false),
                    IsLong = table.Column<bool>(type: "boolean", nullable: false),
                    EntryOrdersInAdvance = table.Column<int>(type: "integer", nullable: false),
                    ExitOrdersInAdvance = table.Column<int>(type: "integer", nullable: false),
                    ExitStep = table.Column<decimal>(type: "numeric", nullable: false),
                    EntryStep = table.Column<decimal>(type: "numeric", nullable: false),
                    EntryQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    StartingBaseAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    StartFromMaxPrice = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tickers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Symbol = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Bid = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    Ask = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    LastPrice = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tickers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Symbol = table.Column<string>(type: "text", nullable: false),
                    Price = table.Column<decimal>(type: "numeric", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    IsBuy = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    QuantityFilled = table.Column<decimal>(type: "numeric", nullable: false),
                    AverageFillPrice = table.Column<decimal>(type: "numeric", nullable: true),
                    Fees = table.Column<decimal>(type: "numeric", nullable: false),
                    Canceled = table.Column<bool>(type: "boolean", nullable: false),
                    Closed = table.Column<bool>(type: "boolean", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EntryTradeId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Trades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ExitOrderId = table.Column<string>(type: "text", nullable: true),
                    Profit = table.Column<decimal>(type: "numeric", nullable: true),
                    BotId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Trades_Bots_BotId",
                        column: x => x.BotId,
                        principalTable: "Bots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Trades_Orders_ExitOrderId",
                        column: x => x.ExitOrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_EntryTradeId",
                table: "Orders",
                column: "EntryTradeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tickers_Symbol_Timestamp",
                table: "Tickers",
                columns: ["Symbol", "Timestamp"]);

            migrationBuilder.CreateIndex(
                name: "IX_Trades_BotId",
                table: "Trades",
                column: "BotId");

            migrationBuilder.CreateIndex(
                name: "IX_Trades_ExitOrderId",
                table: "Trades",
                column: "ExitOrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Trades_EntryTradeId",
                table: "Orders",
                column: "EntryTradeId",
                principalTable: "Trades",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Trades_EntryTradeId",
                table: "Orders");

            migrationBuilder.DropTable(
                name: "Tickers");

            migrationBuilder.DropTable(
                name: "Trades");

            migrationBuilder.DropTable(
                name: "Bots");

            migrationBuilder.DropTable(
                name: "Orders");
        }
    }
}
