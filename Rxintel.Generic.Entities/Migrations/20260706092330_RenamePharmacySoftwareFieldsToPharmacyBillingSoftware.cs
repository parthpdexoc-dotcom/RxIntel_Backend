using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rxintel.Generic.Entities.Migrations
{
    /// <inheritdoc />
    public partial class RenamePharmacySoftwareFieldsToPharmacyBillingSoftware : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PharmacySoftwareSystem",
                table: "Pharmacy",
                newName: "PharmacyBillingSoftwareName");

            migrationBuilder.RenameColumn(
                name: "PharmacySoftwarePhone",
                table: "Pharmacy",
                newName: "PharmacyBillingSoftwarePhone");

            migrationBuilder.RenameColumn(
                name: "PharmacySoftwareEmail",
                table: "Pharmacy",
                newName: "PharmacyBillingSoftwareEmail");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PharmacyBillingSoftwarePhone",
                table: "Pharmacy",
                newName: "PharmacySoftwarePhone");

            migrationBuilder.RenameColumn(
                name: "PharmacyBillingSoftwareName",
                table: "Pharmacy",
                newName: "PharmacySoftwareSystem");

            migrationBuilder.RenameColumn(
                name: "PharmacyBillingSoftwareEmail",
                table: "Pharmacy",
                newName: "PharmacySoftwareEmail");
        }
    }
}
