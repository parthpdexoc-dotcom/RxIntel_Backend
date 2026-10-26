using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rxintel.Generic.Entities.Migrations
{
    /// <inheritdoc />
    public partial class RenameInsuranceGroupParentInsuranceIdToBinNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InsuranceGroup_Insurance_ParentInsuranceId",
                table: "InsuranceGroup");

            migrationBuilder.DropIndex(
                name: "IX_InsuranceGroup_ParentInsuranceId",
                table: "InsuranceGroup");

            migrationBuilder.AddColumn<string>(
                name: "BINNumber",
                table: "InsuranceGroup",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.Sql(@"
UPDATE ig
SET ig.BINNumber = LTRIM(RTRIM(i.BINNumber))
FROM InsuranceGroup ig
INNER JOIN Insurance i ON i.Id = ig.ParentInsuranceId
WHERE i.BINNumber IS NOT NULL AND LTRIM(RTRIM(i.BINNumber)) <> '';
");

            migrationBuilder.Sql(@"
DELETE FROM InsuranceGroup
WHERE BINNumber IS NULL OR LTRIM(RTRIM(BINNumber)) = '';
");

            migrationBuilder.AlterColumn<string>(
                name: "BINNumber",
                table: "InsuranceGroup",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.DropColumn(
                name: "ParentInsuranceId",
                table: "InsuranceGroup");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceGroup_BINNumber",
                table: "InsuranceGroup",
                column: "BINNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InsuranceGroup_BINNumber",
                table: "InsuranceGroup");

            migrationBuilder.AddColumn<Guid>(
                name: "ParentInsuranceId",
                table: "InsuranceGroup",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.Sql(@"
UPDATE ig
SET ig.ParentInsuranceId = parent.Id
FROM InsuranceGroup ig
CROSS APPLY (
    SELECT TOP 1 i.Id
    FROM Insurance i
    WHERE i.BINNumber IS NOT NULL
      AND LTRIM(RTRIM(i.BINNumber)) = LTRIM(RTRIM(ig.BINNumber))
    ORDER BY i.CreatedDate, i.Id
) parent;
");

            migrationBuilder.Sql(@"
DELETE FROM InsuranceGroup
WHERE ParentInsuranceId IS NULL;
");

            migrationBuilder.AlterColumn<Guid>(
                name: "ParentInsuranceId",
                table: "InsuranceGroup",
                type: "uniqueidentifier",
                nullable: false);

            migrationBuilder.DropColumn(
                name: "BINNumber",
                table: "InsuranceGroup");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceGroup_ParentInsuranceId",
                table: "InsuranceGroup",
                column: "ParentInsuranceId");

            migrationBuilder.AddForeignKey(
                name: "FK_InsuranceGroup_Insurance_ParentInsuranceId",
                table: "InsuranceGroup",
                column: "ParentInsuranceId",
                principalTable: "Insurance",
                principalColumn: "Id");
        }
    }
}
