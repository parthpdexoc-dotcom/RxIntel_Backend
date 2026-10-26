using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rxintel.Generic.Entities.Migrations
{
    /// <inheritdoc />
    public partial class AddPharmacyExtendedFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BusinessLegalName",
                table: "Pharmacy",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BusinessTradeName",
                table: "Pharmacy",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PharmacyFax",
                table: "Pharmacy",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeaExpirationDate",
                table: "Pharmacy",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StateBoardOfPharmacyLicenseNumber",
                table: "Pharmacy",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "BopLicenseExpirationDate",
                table: "Pharmacy",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GlobalLocationNumber",
                table: "Pharmacy",
                type: "nvarchar(13)",
                maxLength: 13,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PharmacySoftwareSystem",
                table: "Pharmacy",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PharmacySoftwarePhone",
                table: "Pharmacy",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PharmacySoftwareEmail",
                table: "Pharmacy",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BusinessLegalName",
                table: "Pharmacy");

            migrationBuilder.DropColumn(
                name: "BusinessTradeName",
                table: "Pharmacy");

            migrationBuilder.DropColumn(
                name: "PharmacyFax",
                table: "Pharmacy");

            migrationBuilder.DropColumn(
                name: "DeaExpirationDate",
                table: "Pharmacy");

            migrationBuilder.DropColumn(
                name: "StateBoardOfPharmacyLicenseNumber",
                table: "Pharmacy");

            migrationBuilder.DropColumn(
                name: "BopLicenseExpirationDate",
                table: "Pharmacy");

            migrationBuilder.DropColumn(
                name: "GlobalLocationNumber",
                table: "Pharmacy");

            migrationBuilder.DropColumn(
                name: "PharmacySoftwareSystem",
                table: "Pharmacy");

            migrationBuilder.DropColumn(
                name: "PharmacySoftwarePhone",
                table: "Pharmacy");

            migrationBuilder.DropColumn(
                name: "PharmacySoftwareEmail",
                table: "Pharmacy");
        }
    }
}
