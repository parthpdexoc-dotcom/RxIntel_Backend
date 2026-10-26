using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rxintel.Generic.Entities.Migrations
{
    /// <inheritdoc />
    public partial class RenameMedicineDrugNameFormAndUnitOfMeasure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UnitOfMeasure",
                table: "Medicine",
                newName: "PackageSizeUnit");

            migrationBuilder.RenameColumn(
                name: "Form",
                table: "Medicine",
                newName: "DosageForm");

            migrationBuilder.RenameColumn(
                name: "DrugName",
                table: "Medicine",
                newName: "Brand");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PackageSizeUnit",
                table: "Medicine",
                newName: "UnitOfMeasure");

            migrationBuilder.RenameColumn(
                name: "DosageForm",
                table: "Medicine",
                newName: "Form");

            migrationBuilder.RenameColumn(
                name: "Brand",
                table: "Medicine",
                newName: "DrugName");
        }
    }
}
