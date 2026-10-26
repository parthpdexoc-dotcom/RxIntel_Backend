using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rxintel.Generic.Entities.Migrations
{
    /// <inheritdoc />
    public partial class RenamePharmacyNameToBusinessTradeName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BusinessTradeName",
                table: "Pharmacy");

            migrationBuilder.RenameColumn(
                name: "PharmacyName",
                table: "Pharmacy",
                newName: "BusinessTradeName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "BusinessTradeName",
                table: "Pharmacy",
                newName: "PharmacyName");

            migrationBuilder.AddColumn<string>(
                name: "BusinessTradeName",
                table: "Pharmacy",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }
    }
}
