using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace kazandakazan.Migrations
{
    /// <inheritdoc />
    public partial class DailyDemoWalletLimit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "WalletDailyTopUpDate",
                table: "AspNetUsers",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WalletDailyTopUpUsedAmount",
                table: "AspNetUsers",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WalletDailyTopUpDate",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "WalletDailyTopUpUsedAmount",
                table: "AspNetUsers");
        }
    }
}
