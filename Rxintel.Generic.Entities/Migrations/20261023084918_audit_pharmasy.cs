using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rxintel.Generic.Entities.Migrations
{
    /// <inheritdoc />
    public partial class audit_pharmasy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PharmacyId",
                table: "ErxAuditBatch",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ErxAuditBatch_PharmacyId",
                table: "ErxAuditBatch",
                column: "PharmacyId");

            migrationBuilder.AddForeignKey(
                name: "FK_ErxAuditBatch_Pharmacy_PharmacyId",
                table: "ErxAuditBatch",
                column: "PharmacyId",
                principalTable: "Pharmacy",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ErxAuditBatch_Pharmacy_PharmacyId",
                table: "ErxAuditBatch");

            migrationBuilder.DropIndex(
                name: "IX_ErxAuditBatch_PharmacyId",
                table: "ErxAuditBatch");

            migrationBuilder.DropColumn(
                name: "PharmacyId",
                table: "ErxAuditBatch");
        }
    }
}
