using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Rxintel.Generic.Entities.Entities;

#nullable disable

namespace Rxintel.Generic.Entities.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(RxintelDbContext))]
    [Migration("20260509120000_AddPharmacyWholesaler")]
    public partial class AddPharmacyWholesaler : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PharmacyWholesaler",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PharmacyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WholesalerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PharmacyWholesaler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PharmacyWholesaler_Pharmacy_PharmacyId",
                        column: x => x.PharmacyId,
                        principalTable: "Pharmacy",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PharmacyWholesaler_Wholesaler_WholesalerId",
                        column: x => x.WholesalerId,
                        principalTable: "Wholesaler",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PharmacyWholesaler_PharmacyId",
                table: "PharmacyWholesaler",
                column: "PharmacyId");

            migrationBuilder.CreateIndex(
                name: "IX_PharmacyWholesaler_WholesalerId",
                table: "PharmacyWholesaler",
                column: "WholesalerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PharmacyWholesaler");
        }
    }
}
