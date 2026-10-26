using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rxintel.Generic.Entities.Migrations
{
    /// <inheritdoc />
    public partial class RenamePharmacyAddressColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AddressLine1",
                table: "Pharmacy",
                newName: "PharmacyAddress");

            migrationBuilder.RenameColumn(
                name: "AddressLine2",
                table: "Pharmacy",
                newName: "StreetAddress");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "StreetAddress",
                table: "Pharmacy",
                newName: "AddressLine2");

            migrationBuilder.RenameColumn(
                name: "PharmacyAddress",
                table: "Pharmacy",
                newName: "AddressLine1");
        }
    }
}
