using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Rxintel.Generic.Entities.Entities;

#nullable disable

namespace Rxintel.Generic.Entities.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(RxintelDbContext))]
    [Migration("20260928100000_AddExpiryDateToWholesaler")]
    public partial class AddExpiryDateToWholesaler : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiryDate",
                table: "Wholesaler",
                type: "datetime2",
                nullable: true);

            migrationBuilder.Sql(@"
UPDATE [Wholesaler]
SET [ExpiryDate] = DATEADD(DAY, -1, DATEADD(YEAR, 2, CONVERT(date, [Date])))
WHERE [Date] IS NOT NULL;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpiryDate",
                table: "Wholesaler");
        }
    }
}
