using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rxintel.Generic.Entities.Migrations
{
    /// <inheritdoc />
    public partial class AddPharmacySoftwareIdToPharmacy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PharmacySoftwareId",
                table: "Pharmacy",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pharmacy_PharmacySoftwareId",
                table: "Pharmacy",
                column: "PharmacySoftwareId");

            migrationBuilder.AddForeignKey(
                name: "FK_Pharmacy_PharmacySoftware_PharmacySoftwareId",
                table: "Pharmacy",
                column: "PharmacySoftwareId",
                principalTable: "PharmacySoftware",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Pharmacy_PharmacySoftware_PharmacySoftwareId",
                table: "Pharmacy");

            migrationBuilder.DropIndex(
                name: "IX_Pharmacy_PharmacySoftwareId",
                table: "Pharmacy");

            migrationBuilder.DropColumn(
                name: "PharmacySoftwareId",
                table: "Pharmacy");
        }
    }
}
