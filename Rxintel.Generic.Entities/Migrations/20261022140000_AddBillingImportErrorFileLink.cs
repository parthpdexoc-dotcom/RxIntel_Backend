using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rxintel.Generic.Entities.Migrations
{
    /// <inheritdoc />
    public partial class AddBillingImportErrorFileLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FailedRowCount",
                table: "ImportErrorFile",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalImportedRows",
                table: "ImportErrorFile",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "UploadedBy",
                table: "ImportErrorFile",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ImportErrorFileId",
                table: "Billing",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Billing_ImportErrorFileId",
                table: "Billing",
                column: "ImportErrorFileId");

            migrationBuilder.AddForeignKey(
                name: "FK_Billing_ImportErrorFile_ImportErrorFileId",
                table: "Billing",
                column: "ImportErrorFileId",
                principalTable: "ImportErrorFile",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Billing_ImportErrorFile_ImportErrorFileId",
                table: "Billing");

            migrationBuilder.DropIndex(
                name: "IX_Billing_ImportErrorFileId",
                table: "Billing");

            migrationBuilder.DropColumn(
                name: "ImportErrorFileId",
                table: "Billing");

            migrationBuilder.DropColumn(
                name: "UploadedBy",
                table: "ImportErrorFile");

            migrationBuilder.DropColumn(
                name: "TotalImportedRows",
                table: "ImportErrorFile");

            migrationBuilder.DropColumn(
                name: "FailedRowCount",
                table: "ImportErrorFile");
        }
    }
}
