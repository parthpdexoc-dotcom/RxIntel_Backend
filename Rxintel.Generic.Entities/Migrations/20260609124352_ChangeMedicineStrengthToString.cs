using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rxintel.Generic.Entities.Migrations
{
    /// <inheritdoc />
    public partial class ChangeMedicineStrengthToString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StrengthText",
                table: "Medicine",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE [Medicine]
                SET [StrengthText] = CAST([Strength] AS nvarchar(100))
                WHERE [Strength] IS NOT NULL
                """);

            migrationBuilder.DropColumn(
                name: "Strength",
                table: "Medicine");

            migrationBuilder.RenameColumn(
                name: "StrengthText",
                table: "Medicine",
                newName: "Strength");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StrengthInt",
                table: "Medicine",
                type: "int",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE [Medicine]
                SET [StrengthInt] = TRY_CAST(LTRIM(RTRIM([Strength])) AS int)
                WHERE [Strength] IS NOT NULL
                  AND [Strength] NOT LIKE '%;%'
                  AND TRY_CAST(LTRIM(RTRIM([Strength])) AS int) IS NOT NULL
                """);

            migrationBuilder.DropColumn(
                name: "Strength",
                table: "Medicine");

            migrationBuilder.RenameColumn(
                name: "StrengthInt",
                table: "Medicine",
                newName: "Strength");
        }
    }
}
