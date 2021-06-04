using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace TradeMaster6000.Server.data.migrations.trade
{
    public partial class inisio : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Candles_TradeInstruments_TradeInstrumentId",
                table: "Candles");

            migrationBuilder.DropIndex(
                name: "IX_Candles_TradeInstrumentId",
                table: "Candles");

            migrationBuilder.DropColumn(
                name: "TradeInstrumentId",
                table: "Candles");

            migrationBuilder.AddColumn<uint>(
                name: "InstrumentToken",
                table: "Candles",
                type: "int unsigned",
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.CreateTable(
                name: "OrderUpdates",
                columns: table => new
                {
                    OrderId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    InstrumentToken = table.Column<uint>(type: "int unsigned", nullable: false),
                    AveragePrice = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    FilledQuantity = table.Column<int>(type: "int", nullable: false),
                    TriggerPrice = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RowVersion = table.Column<DateTime>(type: "timestamp(6)", rowVersion: true, nullable: true)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.ComputedColumn)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderUpdates", x => x.OrderId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderUpdates");

            migrationBuilder.DropColumn(
                name: "InstrumentToken",
                table: "Candles");

            migrationBuilder.AddColumn<int>(
                name: "TradeInstrumentId",
                table: "Candles",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Candles_TradeInstrumentId",
                table: "Candles",
                column: "TradeInstrumentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Candles_TradeInstruments_TradeInstrumentId",
                table: "Candles",
                column: "TradeInstrumentId",
                principalTable: "TradeInstruments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
