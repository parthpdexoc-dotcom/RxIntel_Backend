using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rxintel.Generic.Entities.Migrations
{
    /// <inheritdoc />
    public partial class AddPharmacyUserExtendedFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PrimaryContactName",
                table: "PharmacyUser",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrimaryContactTitle",
                table: "PharmacyUser",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrimaryContactPhone",
                table: "PharmacyUser",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrimaryWholesalerName",
                table: "PharmacyUser",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrimaryWholesalerContactName",
                table: "PharmacyUser",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrimaryWholesalerPhone",
                table: "PharmacyUser",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrimaryWholesalerEmail",
                table: "PharmacyUser",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrimaryContactName",
                table: "PharmacyUser");

            migrationBuilder.DropColumn(
                name: "PrimaryContactTitle",
                table: "PharmacyUser");

            migrationBuilder.DropColumn(
                name: "PrimaryContactPhone",
                table: "PharmacyUser");

            migrationBuilder.DropColumn(
                name: "PrimaryWholesalerName",
                table: "PharmacyUser");

            migrationBuilder.DropColumn(
                name: "PrimaryWholesalerContactName",
                table: "PharmacyUser");

            migrationBuilder.DropColumn(
                name: "PrimaryWholesalerPhone",
                table: "PharmacyUser");

            migrationBuilder.DropColumn(
                name: "PrimaryWholesalerEmail",
                table: "PharmacyUser");
        }
    }
}
