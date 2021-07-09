using Microsoft.EntityFrameworkCore.Migrations;

namespace TradeMaster6000.Server.data.migrations.trade
{
    public partial class TheNewstsdd : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Transformed",
                table: "Candles",
                newName: "UsedBy60");

            migrationBuilder.AddColumn<bool>(
                name: "UsedBy15",
                table: "Candles",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "UsedBy30",
                table: "Candles",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "UsedBy45",
                table: "Candles",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "UsedBy5",
                table: "Candles",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UsedBy15",
                table: "Candles");

            migrationBuilder.DropColumn(
                name: "UsedBy30",
                table: "Candles");

            migrationBuilder.DropColumn(
                name: "UsedBy45",
                table: "Candles");

            migrationBuilder.DropColumn(
                name: "UsedBy5",
                table: "Candles");

            migrationBuilder.RenameColumn(
                name: "UsedBy60",
                table: "Candles",
                newName: "Transformed");
        }
    }
}
