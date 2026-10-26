using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rxintel.Generic.Entities.Migrations
{
    /// <inheritdoc />
    public partial class renamecolumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InsuranceGroup_Insurance_ChildInsurnaceId",
                table: "InsuranceGroup");

            migrationBuilder.RenameColumn(
                name: "ChildInsurnaceId",
                table: "InsuranceGroup",
                newName: "ChildInsuranceId");

            migrationBuilder.RenameIndex(
                name: "IX_InsuranceGroup_ChildInsurnaceId",
                table: "InsuranceGroup",
                newName: "IX_InsuranceGroup_ChildInsuranceId");

            migrationBuilder.AddForeignKey(
                name: "FK_InsuranceGroup_Insurance_ChildInsuranceId",
                table: "InsuranceGroup",
                column: "ChildInsuranceId",
                principalTable: "Insurance",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InsuranceGroup_Insurance_ChildInsuranceId",
                table: "InsuranceGroup");

            migrationBuilder.RenameColumn(
                name: "ChildInsuranceId",
                table: "InsuranceGroup",
                newName: "ChildInsurnaceId");

            migrationBuilder.RenameIndex(
                name: "IX_InsuranceGroup_ChildInsuranceId",
                table: "InsuranceGroup",
                newName: "IX_InsuranceGroup_ChildInsurnaceId");

            migrationBuilder.AddForeignKey(
                name: "FK_InsuranceGroup_Insurance_ChildInsurnaceId",
                table: "InsuranceGroup",
                column: "ChildInsurnaceId",
                principalTable: "Insurance",
                principalColumn: "Id");
        }
    }
}
