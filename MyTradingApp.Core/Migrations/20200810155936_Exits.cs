using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MyTradingApp.Core.Migrations
{
    public partial class Exits : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Exit",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TradeId = table.Column<int>(nullable: false),
                    TimeStamp = table.Column<DateTime>(nullable: false),
                    Price = table.Column<double>(nullable: false),
                    Quantity = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Exit", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Exit_Trade_TradeId",
                        column: x => x.TradeId,
                        principalTable: "Trade",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Exit_TradeId",
                table: "Exit",
                column: "TradeId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Exit");
        }
    }
}
