using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rxintel.Generic.Entities.Migrations
{
    /// <inheritdoc />
    public partial class MakeMedicineNdcUniqueAndPackageSizeRequired : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE [Medicine] SET [PackageSize] = '' WHERE [PackageSize] IS NULL");

            migrationBuilder.Sql("""
                WITH RankedMedicine AS (
                    SELECT
                        Id,
                        NDC,
                        ROW_NUMBER() OVER (PARTITION BY NDC ORDER BY CreatedDate, Id) AS RowNum
                    FROM [Medicine]
                )
                DELETE m
                FROM [Medicine] m
                INNER JOIN RankedMedicine r ON m.Id = r.Id
                WHERE r.RowNum > 1
                  AND NOT EXISTS (
                      SELECT 1
                      FROM [NDCGroup] g
                      WHERE g.ParentMedicineId = m.Id OR g.ChildMedicineId = m.Id);
                """);

            migrationBuilder.Sql("""
                WITH RankedMedicine AS (
                    SELECT
                        Id,
                        NDC,
                        ROW_NUMBER() OVER (PARTITION BY NDC ORDER BY CreatedDate, Id) AS RowNum
                    FROM [Medicine]
                )
                UPDATE m
                SET m.NDC = LEFT(r.NDC, 88) + '-' + LEFT(REPLACE(CAST(r.Id AS nvarchar(36)), '-', ''), 8)
                FROM [Medicine] m
                INNER JOIN RankedMedicine r ON m.Id = r.Id
                WHERE r.RowNum > 1;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "PackageSize",
                table: "Medicine",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Medicine_NDC",
                table: "Medicine",
                column: "NDC",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Medicine_NDC",
                table: "Medicine");

            migrationBuilder.AlterColumn<string>(
                name: "PackageSize",
                table: "Medicine",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);
        }
    }
}
