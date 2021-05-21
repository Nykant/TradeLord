using Microsoft.EntityFrameworkCore.Migrations;

namespace TradeMaster6000.Server.data.migrations.trade
{
    public partial class two : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EntryHit",
                table: "TradeOrders");

            migrationBuilder.DropColumn(
                name: "SLMHit",
                table: "TradeOrders");

            migrationBuilder.RenameColumn(
                name: "TargetHit",
                table: "TradeOrders",
                newName: "PreSLMCancelled");

            migrationBuilder.AddColumn<string>(
                name: "EntryId",
                table: "TradeOrders",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "EntryStatus",
                table: "TradeOrders",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "SLMId",
                table: "TradeOrders",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "SLMStatus",
                table: "TradeOrders",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "Target",
                table: "TradeOrders",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "TargetId",
                table: "TradeOrders",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "TargetStatus",
                table: "TradeOrders",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EntryId",
                table: "TradeOrders");

            migrationBuilder.DropColumn(
                name: "EntryStatus",
                table: "TradeOrders");

            migrationBuilder.DropColumn(
                name: "SLMId",
                table: "TradeOrders");

            migrationBuilder.DropColumn(
                name: "SLMStatus",
                table: "TradeOrders");

            migrationBuilder.DropColumn(
                name: "Target",
                table: "TradeOrders");

            migrationBuilder.DropColumn(
                name: "TargetId",
                table: "TradeOrders");

            migrationBuilder.DropColumn(
                name: "TargetStatus",
                table: "TradeOrders");

            migrationBuilder.RenameColumn(
                name: "PreSLMCancelled",
                table: "TradeOrders",
                newName: "TargetHit");

            migrationBuilder.AddColumn<bool>(
                name: "EntryHit",
                table: "TradeOrders",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SLMHit",
                table: "TradeOrders",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }
    }
}
