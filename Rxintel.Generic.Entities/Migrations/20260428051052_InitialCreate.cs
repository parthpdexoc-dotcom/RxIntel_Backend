using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rxintel.Generic.Entities.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InsuranceGroup",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GroupCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    GroupName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InsuranceGroup", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NDCGroup",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GroupName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Strength = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NDCGroup", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Pharmacy",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PharmacyCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PharmacyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NPI = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NABP = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TaxId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AddressLine1 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AddressLine2 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StateCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ZipCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pharmacy", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Insurance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InsuranceGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InsuranceName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BINNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Insurance", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Insurance_InsuranceGroup_InsuranceGroupId",
                        column: x => x.InsuranceGroupId,
                        principalTable: "InsuranceGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MedicineMaster",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NDCGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NDC = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DrugName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Strength = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UnitType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PacketSize = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Form = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StrengthType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicineMaster", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicineMaster_NDCGroup_NDCGroupId",
                        column: x => x.NDCGroupId,
                        principalTable: "NDCGroup",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PharmacyUser",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PharmacyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    RoleName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PharmacyUser", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PharmacyUser_Pharmacy_PharmacyId",
                        column: x => x.PharmacyId,
                        principalTable: "Pharmacy",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Insurance_InsuranceGroupId",
                table: "Insurance",
                column: "InsuranceGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicineMaster_NDCGroupId",
                table: "MedicineMaster",
                column: "NDCGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_PharmacyUser_PharmacyId",
                table: "PharmacyUser",
                column: "PharmacyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Insurance");

            migrationBuilder.DropTable(
                name: "MedicineMaster");

            migrationBuilder.DropTable(
                name: "PharmacyUser");

            migrationBuilder.DropTable(
                name: "InsuranceGroup");

            migrationBuilder.DropTable(
                name: "NDCGroup");

            migrationBuilder.DropTable(
                name: "Pharmacy");
        }
    }
}
