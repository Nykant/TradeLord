using Microsoft.EntityFrameworkCore.Migrations;

namespace TradeMaster6000.Server.data.migrations.trade
{
    public partial class three : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExitTransactionType",
                table: "TradeOrders",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "IsOrderFilling",
                table: "TradeOrders",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RegularSlmPlaced",
                table: "TradeOrders",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SlRejected",
                table: "TradeOrders",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "TargetHit",
                table: "TradeOrders",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "TargetPlaced",
                table: "TradeOrders",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "ZoneWidth",
                table: "TradeOrders",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExitTransactionType",
                table: "TradeOrders");

            migrationBuilder.DropColumn(
                name: "IsOrderFilling",
                table: "TradeOrders");

            migrationBuilder.DropColumn(
                name: "RegularSlmPlaced",
                table: "TradeOrders");

            migrationBuilder.DropColumn(
                name: "SlRejected",
                table: "TradeOrders");

            migrationBuilder.DropColumn(
                name: "TargetHit",
                table: "TradeOrders");

            migrationBuilder.DropColumn(
                name: "TargetPlaced",
                table: "TradeOrders");

            migrationBuilder.DropColumn(
                name: "ZoneWidth",
                table: "TradeOrders");
        }
    }
}
