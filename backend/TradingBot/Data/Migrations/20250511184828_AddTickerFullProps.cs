using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradingBot.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTickerFullProps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CloseTime",
                table: "Tickers",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<long>(
                name: "FirstTradeId",
                table: "Tickers",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<decimal>(
                name: "HighPrice",
                table: "Tickers",
                type: "numeric(18,8)",
                precision: 18,
                scale: 8,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<long>(
                name: "LastTradeId",
                table: "Tickers",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<decimal>(
                name: "LowPrice",
                table: "Tickers",
                type: "numeric(18,8)",
                precision: 18,
                scale: 8,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OpenPrice",
                table: "Tickers",
                type: "numeric(18,8)",
                precision: 18,
                scale: 8,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "OpenTime",
                table: "Tickers",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<decimal>(
                name: "PriceChange",
                table: "Tickers",
                type: "numeric(18,8)",
                precision: 18,
                scale: 8,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PriceChangePercent",
                table: "Tickers",
                type: "numeric(18,8)",
                precision: 18,
                scale: 8,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "QuoteVolume",
                table: "Tickers",
                type: "numeric(18,8)",
                precision: 18,
                scale: 8,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<long>(
                name: "TotalTrades",
                table: "Tickers",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<decimal>(
                name: "Volume",
                table: "Tickers",
                type: "numeric(18,8)",
                precision: 18,
                scale: 8,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "WeightedAveragePrice",
                table: "Tickers",
                type: "numeric(18,8)",
                precision: 18,
                scale: 8,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CloseTime",
                table: "Tickers");

            migrationBuilder.DropColumn(
                name: "FirstTradeId",
                table: "Tickers");

            migrationBuilder.DropColumn(
                name: "HighPrice",
                table: "Tickers");

            migrationBuilder.DropColumn(
                name: "LastTradeId",
                table: "Tickers");

            migrationBuilder.DropColumn(
                name: "LowPrice",
                table: "Tickers");

            migrationBuilder.DropColumn(
                name: "OpenPrice",
                table: "Tickers");

            migrationBuilder.DropColumn(
                name: "OpenTime",
                table: "Tickers");

            migrationBuilder.DropColumn(
                name: "PriceChange",
                table: "Tickers");

            migrationBuilder.DropColumn(
                name: "PriceChangePercent",
                table: "Tickers");

            migrationBuilder.DropColumn(
                name: "QuoteVolume",
                table: "Tickers");

            migrationBuilder.DropColumn(
                name: "TotalTrades",
                table: "Tickers");

            migrationBuilder.DropColumn(
                name: "Volume",
                table: "Tickers");

            migrationBuilder.DropColumn(
                name: "WeightedAveragePrice",
                table: "Tickers");
        }
    }
}
