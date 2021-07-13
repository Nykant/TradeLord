using Microsoft.EntityFrameworkCore.Migrations;

namespace TradeMaster6000.Server.data.migrations.trade
{
    public partial class Dudulidu : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Entry",
                table: "Zones",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "UsedBy10",
                table: "Candles",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "UsedBy120",
                table: "Candles",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "UsedBy240",
                table: "Candles",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "UsedByCurve",
                table: "Candles",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "UsedByEntry",
                table: "Candles",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "UsedForCurve",
                table: "Candles",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "UsedForEntry",
                table: "Candles",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Entry",
                table: "Zones");

            migrationBuilder.DropColumn(
                name: "UsedBy10",
                table: "Candles");

            migrationBuilder.DropColumn(
                name: "UsedBy120",
                table: "Candles");

            migrationBuilder.DropColumn(
                name: "UsedBy240",
                table: "Candles");

            migrationBuilder.DropColumn(
                name: "UsedByCurve",
                table: "Candles");

            migrationBuilder.DropColumn(
                name: "UsedByEntry",
                table: "Candles");

            migrationBuilder.DropColumn(
                name: "UsedForCurve",
                table: "Candles");

            migrationBuilder.DropColumn(
                name: "UsedForEntry",
                table: "Candles");
        }
    }
}
