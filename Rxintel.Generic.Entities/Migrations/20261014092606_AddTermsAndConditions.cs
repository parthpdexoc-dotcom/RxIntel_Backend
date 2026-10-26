using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rxintel.Generic.Entities.Migrations
{
    /// <inheritdoc />
    public partial class AddTermsAndConditions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TermsAndConditions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedByAdminId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TermsAndConditions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TermsAndConditions_Admin_UpdatedByAdminId",
                        column: x => x.UpdatedByAdminId,
                        principalTable: "Admin",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TermsAcceptance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PharmacyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PharmacyUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TermsAndConditionsId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TermsAcceptance", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TermsAcceptance_PharmacyUser_PharmacyUserId",
                        column: x => x.PharmacyUserId,
                        principalTable: "PharmacyUser",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TermsAcceptance_Pharmacy_PharmacyId",
                        column: x => x.PharmacyId,
                        principalTable: "Pharmacy",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TermsAcceptance_TermsAndConditions_TermsAndConditionsId",
                        column: x => x.TermsAndConditionsId,
                        principalTable: "TermsAndConditions",
                        principalColumn: "Id");
                });

            migrationBuilder.Sql(@"
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Medicine_Brand' AND object_id = OBJECT_ID(N'[Medicine]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Medicine_Brand] ON [Medicine] ([Brand]);
END");

            migrationBuilder.CreateIndex(
                name: "IX_TermsAcceptance_PharmacyId",
                table: "TermsAcceptance",
                column: "PharmacyId");

            migrationBuilder.CreateIndex(
                name: "IX_TermsAcceptance_PharmacyUserId",
                table: "TermsAcceptance",
                column: "PharmacyUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TermsAcceptance_TermsAndConditionsId",
                table: "TermsAcceptance",
                column: "TermsAndConditionsId");

            migrationBuilder.CreateIndex(
                name: "IX_TermsAndConditions_UpdatedByAdminId",
                table: "TermsAndConditions",
                column: "UpdatedByAdminId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TermsAcceptance");

            migrationBuilder.DropTable(
                name: "TermsAndConditions");

            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Medicine_Brand' AND object_id = OBJECT_ID(N'[Medicine]'))
BEGIN
    DROP INDEX [IX_Medicine_Brand] ON [Medicine];
END");
        }
    }
}
