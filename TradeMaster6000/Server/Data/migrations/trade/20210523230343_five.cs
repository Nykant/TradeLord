using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace TradeMaster6000.Server.data.migrations.trade
{
    public partial class five : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "SquaredOff",
                table: "TradeOrders",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Candles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TradeInstrumentId = table.Column<int>(type: "int", nullable: true),
                    Open = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    High = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Low = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Close = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    From = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    To = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Candles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Candles_TradeInstruments_TradeInstrumentId",
                        column: x => x.TradeInstrumentId,
                        principalTable: "TradeInstruments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Candles_TradeInstrumentId",
                table: "Candles",
                column: "TradeInstrumentId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Candles");

            migrationBuilder.DropColumn(
                name: "SquaredOff",
                table: "TradeOrders");
        }
    }
}
