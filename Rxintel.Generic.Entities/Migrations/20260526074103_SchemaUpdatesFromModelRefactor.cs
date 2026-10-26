using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rxintel.Generic.Entities.Migrations
{
    /// <inheritdoc />
    public partial class SchemaUpdatesFromModelRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NABP",
                table: "Pharmacy");

            migrationBuilder.DropColumn(
                name: "TaxId",
                table: "Pharmacy");

            migrationBuilder.DropColumn(
                name: "InsuranceName",
                table: "Billing");

            migrationBuilder.DropColumn(
                name: "PrimaryInsurance",
                table: "Billing");

            migrationBuilder.RenameColumn(
                name: "UnitSize",
                table: "Purchase",
                newName: "PackageSize");

            migrationBuilder.RenameColumn(
                name: "ZipCode",
                table: "Pharmacy",
                newName: "Zip");

            migrationBuilder.RenameColumn(
                name: "PharmacyCode",
                table: "Pharmacy",
                newName: "DEA");

            migrationBuilder.RenameColumn(
                name: "StrengthType",
                table: "Medicine",
                newName: "UnitOfMeasure");

            migrationBuilder.RenameColumn(
                name: "PacketSize",
                table: "Medicine",
                newName: "PackageSize");

            migrationBuilder.RenameColumn(
                name: "FinalizedDate",
                table: "Billing",
                newName: "BillDate");

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Wholesaler",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Wholesaler",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "Wholesaler",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Purchase",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Insurance",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "Insurance",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "Wholesaler");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Wholesaler");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "Wholesaler");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Purchase");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Insurance");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "Insurance");

            migrationBuilder.RenameColumn(
                name: "PackageSize",
                table: "Purchase",
                newName: "UnitSize");

            migrationBuilder.RenameColumn(
                name: "Zip",
                table: "Pharmacy",
                newName: "ZipCode");

            migrationBuilder.RenameColumn(
                name: "DEA",
                table: "Pharmacy",
                newName: "PharmacyCode");

            migrationBuilder.RenameColumn(
                name: "UnitOfMeasure",
                table: "Medicine",
                newName: "StrengthType");

            migrationBuilder.RenameColumn(
                name: "PackageSize",
                table: "Medicine",
                newName: "PacketSize");

            migrationBuilder.RenameColumn(
                name: "BillDate",
                table: "Billing",
                newName: "FinalizedDate");

            migrationBuilder.AddColumn<string>(
                name: "NABP",
                table: "Pharmacy",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxId",
                table: "Pharmacy",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InsuranceName",
                table: "Billing",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PrimaryInsurance",
                table: "Billing",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }
    }
}
