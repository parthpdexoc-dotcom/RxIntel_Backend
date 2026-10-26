using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Rxintel.Generic.Entities.Entities;

#nullable disable

namespace Rxintel.Generic.Entities.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(RxintelDbContext))]
    [Migration("20260928103000_RenameWholesalerDateToVawdDate")]
    public partial class RenameWholesalerDateToVawdDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Date",
                table: "Wholesaler",
                newName: "VawdDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "VawdDate",
                table: "Wholesaler",
                newName: "Date");
        }
    }
}
