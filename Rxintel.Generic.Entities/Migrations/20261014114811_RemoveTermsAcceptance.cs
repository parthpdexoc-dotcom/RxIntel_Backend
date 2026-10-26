using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rxintel.Generic.Entities.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTermsAcceptance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TermsAcceptance");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
        }
    }
}
