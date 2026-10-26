using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rxintel.Generic.Entities.Migrations
{
    /// <inheritdoc />
    public partial class SplitImportErrorAndRenamePharmacyBillingSoftware : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BillingFieldMapping_PharmacySoftware_PharmacySoftwareId",
                table: "BillingFieldMapping");

            migrationBuilder.DropForeignKey(
                name: "FK_Pharmacy_PharmacySoftware_PharmacySoftwareId",
                table: "Pharmacy");

            migrationBuilder.RenameTable(
                name: "PharmacySoftware",
                newName: "PharmacyBillingSoftware");

            migrationBuilder.RenameTable(
                name: "ImportError",
                newName: "ImportErrorFile");

            migrationBuilder.DropColumn(
                name: "ErrorDetailsJson",
                table: "ImportErrorFile");

            migrationBuilder.CreateTable(
                name: "ImportErrorDetail",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Row = table.Column<int>(type: "int", nullable: false),
                    ErrorType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NDC = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RxNoOrInvoice = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ErrorMessagesJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportErrorDetail", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportErrorDetail_ImportErrorFile_FileId",
                        column: x => x.FileId,
                        principalTable: "ImportErrorFile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImportErrorDetail_FileId",
                table: "ImportErrorDetail",
                column: "FileId");

            migrationBuilder.AddForeignKey(
                name: "FK_BillingFieldMapping_PharmacyBillingSoftware_PharmacySoftwareId",
                table: "BillingFieldMapping",
                column: "PharmacySoftwareId",
                principalTable: "PharmacyBillingSoftware",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Pharmacy_PharmacyBillingSoftware_PharmacySoftwareId",
                table: "Pharmacy",
                column: "PharmacySoftwareId",
                principalTable: "PharmacyBillingSoftware",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BillingFieldMapping_PharmacyBillingSoftware_PharmacySoftwareId",
                table: "BillingFieldMapping");

            migrationBuilder.DropForeignKey(
                name: "FK_Pharmacy_PharmacyBillingSoftware_PharmacySoftwareId",
                table: "Pharmacy");

            migrationBuilder.DropTable(
                name: "ImportErrorDetail");

            migrationBuilder.AddColumn<string>(
                name: "ErrorDetailsJson",
                table: "ImportErrorFile",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.RenameTable(
                name: "ImportErrorFile",
                newName: "ImportError");

            migrationBuilder.RenameTable(
                name: "PharmacyBillingSoftware",
                newName: "PharmacySoftware");

            migrationBuilder.AddForeignKey(
                name: "FK_BillingFieldMapping_PharmacySoftware_PharmacySoftwareId",
                table: "BillingFieldMapping",
                column: "PharmacySoftwareId",
                principalTable: "PharmacySoftware",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Pharmacy_PharmacySoftware_PharmacySoftwareId",
                table: "Pharmacy",
                column: "PharmacySoftwareId",
                principalTable: "PharmacySoftware",
                principalColumn: "Id");
        }
    }
}
