using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rxintel.Generic.Entities.Migrations
{
    /// <inheritdoc />
    public partial class AddMedicineProductNdcAndLabelerFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Generic",
                table: "Medicine",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Labeler",
                table: "Medicine",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductNDC",
                table: "Medicine",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Route",
                table: "Medicine",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Suffix",
                table: "Medicine",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Generic",
                table: "Medicine");

            migrationBuilder.DropColumn(
                name: "Labeler",
                table: "Medicine");

            migrationBuilder.DropColumn(
                name: "ProductNDC",
                table: "Medicine");

            migrationBuilder.DropColumn(
                name: "Route",
                table: "Medicine");

            migrationBuilder.DropColumn(
                name: "Suffix",
                table: "Medicine");
        }
    }
}
