using Microsoft.EntityFrameworkCore.Migrations;

namespace TradeMaster6000.Server.data.migrations.trade
{
    public partial class DaOne : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Nested",
                table: "Zones",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SupplyDemand",
                table: "Zones",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "Tradeable",
                table: "Zones",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Timeframe",
                table: "Candles",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Nested",
                table: "Zones");

            migrationBuilder.DropColumn(
                name: "SupplyDemand",
                table: "Zones");

            migrationBuilder.DropColumn(
                name: "Tradeable",
                table: "Zones");

            migrationBuilder.DropColumn(
                name: "Timeframe",
                table: "Candles");
        }
    }
}
