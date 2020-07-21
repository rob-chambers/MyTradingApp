using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MyTradingApp.Core.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Setting",
                columns: table => new
                {
                    varchar25 = table.Column<string>(name: "varchar(25)", nullable: false),
                    Value = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Setting", x => x.varchar25);
                });

            migrationBuilder.CreateTable(
                name: "Trade",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Symbol = table.Column<string>(type: "varchar(6)", nullable: true),
                    EntryTimeStamp = table.Column<DateTime>(nullable: false),
                    EntryPrice = table.Column<double>(nullable: false),
                    Direction = table.Column<byte>(nullable: false),
                    Quantity = table.Column<int>(nullable: false),
                    ExitTimeStamp = table.Column<DateTime>(nullable: true),
                    ExitPrice = table.Column<double>(nullable: true),
                    ProfitLoss = table.Column<double>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trade", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StopLoss",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TradeId = table.Column<int>(nullable: false),
                    TimeStamp = table.Column<DateTime>(nullable: false),
                    Price = table.Column<double>(nullable: false),
                    StopPrice = table.Column<double>(nullable: false),
                    StopType = table.Column<byte>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StopLoss", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StopLoss_Trade_TradeId",
                        column: x => x.TradeId,
                        principalTable: "Trade",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StopLoss_TradeId",
                table: "StopLoss",
                column: "TradeId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Setting");

            migrationBuilder.DropTable(
                name: "StopLoss");

            migrationBuilder.DropTable(
                name: "Trade");
        }
    }
}
