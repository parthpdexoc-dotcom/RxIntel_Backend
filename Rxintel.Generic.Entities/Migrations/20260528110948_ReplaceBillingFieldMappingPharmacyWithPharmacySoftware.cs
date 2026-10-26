using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rxintel.Generic.Entities.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceBillingFieldMappingPharmacyWithPharmacySoftware : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BillingFieldMapping_Pharmacy_PharmacyId",
                table: "BillingFieldMapping");

            migrationBuilder.RenameColumn(
                name: "PharmacyId",
                table: "BillingFieldMapping",
                newName: "PharmacySoftwareId");

            migrationBuilder.RenameIndex(
                name: "IX_BillingFieldMapping_PharmacyId",
                table: "BillingFieldMapping",
                newName: "IX_BillingFieldMapping_PharmacySoftwareId");

            migrationBuilder.Sql(@"
                INSERT INTO [PharmacySoftware] ([Id], [SoftwareName])
                SELECT DISTINCT b.[PharmacySoftwareId], CONCAT('Migrated-', CONVERT(varchar(36), b.[PharmacySoftwareId]))
                FROM [BillingFieldMapping] b
                LEFT JOIN [PharmacySoftware] ps ON ps.[Id] = b.[PharmacySoftwareId]
                WHERE ps.[Id] IS NULL;");

            migrationBuilder.AddForeignKey(
                name: "FK_BillingFieldMapping_PharmacySoftware_PharmacySoftwareId",
                table: "BillingFieldMapping",
                column: "PharmacySoftwareId",
                principalTable: "PharmacySoftware",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BillingFieldMapping_PharmacySoftware_PharmacySoftwareId",
                table: "BillingFieldMapping");

            migrationBuilder.RenameColumn(
                name: "PharmacySoftwareId",
                table: "BillingFieldMapping",
                newName: "PharmacyId");

            migrationBuilder.RenameIndex(
                name: "IX_BillingFieldMapping_PharmacySoftwareId",
                table: "BillingFieldMapping",
                newName: "IX_BillingFieldMapping_PharmacyId");

            migrationBuilder.AddForeignKey(
                name: "FK_BillingFieldMapping_Pharmacy_PharmacyId",
                table: "BillingFieldMapping",
                column: "PharmacyId",
                principalTable: "Pharmacy",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
