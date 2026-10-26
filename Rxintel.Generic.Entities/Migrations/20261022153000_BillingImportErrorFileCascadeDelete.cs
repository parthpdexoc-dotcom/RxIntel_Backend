using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Rxintel.Generic.Entities.Entities;

#nullable disable

namespace Rxintel.Generic.Entities.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(RxintelDbContext))]
    [Migration("20261022153000_BillingImportErrorFileCascadeDelete")]
    public partial class BillingImportErrorFileCascadeDelete : Migration    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Billing_ImportErrorFile_ImportErrorFileId",
                table: "Billing");

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

            migrationBuilder.AddForeignKey(
                name: "FK_Billing_ImportErrorFile_ImportErrorFileId",
                table: "Billing",
                column: "ImportErrorFileId",
                principalTable: "ImportErrorFile",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
