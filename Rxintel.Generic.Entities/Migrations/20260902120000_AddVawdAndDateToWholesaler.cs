using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Rxintel.Generic.Entities.Entities;

#nullable disable

namespace Rxintel.Generic.Entities.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(RxintelDbContext))]
    [Migration("20260902120000_AddVawdAndDateToWholesaler")]
    public partial class AddVawdAndDateToWholesaler : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Vawd",
                table: "Wholesaler",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "Date",
                table: "Wholesaler",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Vawd",
                table: "Wholesaler");

            migrationBuilder.DropColumn(
                name: "Date",
                table: "Wholesaler");
        }
    }
}
