using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rxintel.Generic.Entities.Migrations
{
    /// <inheritdoc />
    public partial class AddTotalQuantityOrderedToPurchase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TotalQuantityOrdered",
                table: "Purchase",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql("""
                UPDATE Purchase
                SET TotalQuantityOrdered = QuantityPurchased * TRY_CAST(PackageSize AS decimal(18, 4))
                WHERE PackageSize IS NOT NULL
                  AND LTRIM(RTRIM(PackageSize)) <> ''
                  AND TRY_CAST(PackageSize AS decimal(18, 4)) IS NOT NULL
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalQuantityOrdered",
                table: "Purchase");
        }
    }
}
