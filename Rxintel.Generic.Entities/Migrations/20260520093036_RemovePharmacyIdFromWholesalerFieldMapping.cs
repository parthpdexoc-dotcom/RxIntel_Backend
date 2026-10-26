using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rxintel.Generic.Entities.Migrations
{
    /// <inheritdoc />
    public partial class RemovePharmacyIdFromWholesalerFieldMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // One row per (WholesalerId, SourceColumnName); drop extras before removing PharmacyId.
            migrationBuilder.Sql(
                """
                WITH dupes AS (
                    SELECT Id,
                           ROW_NUMBER() OVER (
                               PARTITION BY WholesalerId, SourceColumnName
                               ORDER BY CreatedDate ASC, Id ASC) AS rn
                    FROM WholesalerFieldMapping
                )
                DELETE FROM WholesalerFieldMapping
                WHERE Id IN (SELECT Id FROM dupes WHERE rn > 1);
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_WholesalerFieldMapping_Pharmacy_PharmacyId",
                table: "WholesalerFieldMapping");

            migrationBuilder.DropIndex(
                name: "IX_WholesalerFieldMapping_PharmacyId",
                table: "WholesalerFieldMapping");

            migrationBuilder.DropColumn(
                name: "PharmacyId",
                table: "WholesalerFieldMapping");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PharmacyId",
                table: "WholesalerFieldMapping",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_WholesalerFieldMapping_PharmacyId",
                table: "WholesalerFieldMapping",
                column: "PharmacyId");

            migrationBuilder.AddForeignKey(
                name: "FK_WholesalerFieldMapping_Pharmacy_PharmacyId",
                table: "WholesalerFieldMapping",
                column: "PharmacyId",
                principalTable: "Pharmacy",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
