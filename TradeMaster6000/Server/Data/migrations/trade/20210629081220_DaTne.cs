using Microsoft.EntityFrameworkCore.Migrations;

namespace TradeMaster6000.Server.data.migrations.trade
{
    public partial class DaTne : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Nested",
                table: "Zones");

            migrationBuilder.AlterColumn<int>(
                name: "Tradeable",
                table: "Zones",
                type: "int",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "tinyint(1)");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "Tradeable",
                table: "Zones",
                type: "tinyint(1)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "Nested",
                table: "Zones",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
