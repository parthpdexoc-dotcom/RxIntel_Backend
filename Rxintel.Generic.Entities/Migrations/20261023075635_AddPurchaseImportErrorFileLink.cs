using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rxintel.Generic.Entities.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseImportErrorFileLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ImportErrorFileId",
                table: "Purchase",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Purchase_ImportErrorFileId",
                table: "Purchase",
                column: "ImportErrorFileId");

            migrationBuilder.AddForeignKey(
                name: "FK_Purchase_ImportErrorFile_ImportErrorFileId",
                table: "Purchase",
                column: "ImportErrorFileId",
                principalTable: "ImportErrorFile",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Purchase_ImportErrorFile_ImportErrorFileId",
                table: "Purchase");

            migrationBuilder.DropIndex(
                name: "IX_Purchase_ImportErrorFileId",
                table: "Purchase");

            migrationBuilder.DropColumn(
                name: "ImportErrorFileId",
                table: "Purchase");
        }
    }
}
