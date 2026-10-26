using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rxintel.Generic.Entities.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedDatabaseSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Billing_Insurance_InsuranceId",
                table: "Billing");

            migrationBuilder.DropForeignKey(
                name: "FK_Insurance_InsuranceGroup_InsuranceGroupId",
                table: "Insurance");

            migrationBuilder.DropTable(
                name: "MedicineMaster");

            migrationBuilder.DropIndex(
                name: "IX_Insurance_InsuranceGroupId",
                table: "Insurance");

            migrationBuilder.DropColumn(
                name: "GroupName",
                table: "NDCGroup");

            migrationBuilder.DropColumn(
                name: "Strength",
                table: "NDCGroup");

            migrationBuilder.DropColumn(
                name: "GroupCode",
                table: "InsuranceGroup");

            migrationBuilder.DropColumn(
                name: "GroupName",
                table: "InsuranceGroup");

            migrationBuilder.DropColumn(
                name: "InsuranceGroupId",
                table: "Insurance");

            migrationBuilder.RenameColumn(
                name: "SourceColumnName",
                table: "BillingFieldMapping",
                newName: "SourceFieldName");

            migrationBuilder.RenameColumn(
                name: "CanonicalFieldName",
                table: "BillingFieldMapping",
                newName: "DestinationFieldName");

            migrationBuilder.AddColumn<Guid>(
                name: "ChildMedicineId",
                table: "NDCGroup",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ParentMedicineId",
                table: "NDCGroup",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ChildInsurnaceId",
                table: "InsuranceGroup",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ParentInsuranceId",
                table: "InsuranceGroup",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<Guid>(
                name: "InsuranceId",
                table: "Billing",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Days",
                table: "Billing",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DrugForm",
                table: "Billing",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DrugName",
                table: "Billing",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DrugTherapy",
                table: "Billing",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FinalizedDate",
                table: "Billing",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "InsName",
                table: "Billing",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PackageSize",
                table: "Billing",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Priins",
                table: "Billing",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PriinsBinNo",
                table: "Billing",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "PriinsPaid",
                table: "Billing",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Quant",
                table: "Billing",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "RXNO",
                table: "Billing",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Secins",
                table: "Billing",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecinsBinNo",
                table: "Billing",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "SecinsPaid",
                table: "Billing",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalInsPaid",
                table: "Billing",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "Medicine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NDC = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DrugName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Strength = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UnitType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Unit = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PacketSize = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Form = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    StrengthType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Medicine", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NDCGroup_ChildMedicineId",
                table: "NDCGroup",
                column: "ChildMedicineId");

            migrationBuilder.CreateIndex(
                name: "IX_NDCGroup_ParentMedicineId",
                table: "NDCGroup",
                column: "ParentMedicineId");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceGroup_ChildInsurnaceId",
                table: "InsuranceGroup",
                column: "ChildInsurnaceId");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceGroup_ParentInsuranceId",
                table: "InsuranceGroup",
                column: "ParentInsuranceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Billing_Insurance_InsuranceId",
                table: "Billing",
                column: "InsuranceId",
                principalTable: "Insurance",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_InsuranceGroup_Insurance_ChildInsurnaceId",
                table: "InsuranceGroup",
                column: "ChildInsurnaceId",
                principalTable: "Insurance",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InsuranceGroup_Insurance_ParentInsuranceId",
                table: "InsuranceGroup",
                column: "ParentInsuranceId",
                principalTable: "Insurance",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_NDCGroup_Medicine_ChildMedicineId",
                table: "NDCGroup",
                column: "ChildMedicineId",
                principalTable: "Medicine",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_NDCGroup_Medicine_ParentMedicineId",
                table: "NDCGroup",
                column: "ParentMedicineId",
                principalTable: "Medicine",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Billing_Insurance_InsuranceId",
                table: "Billing");

            migrationBuilder.DropForeignKey(
                name: "FK_InsuranceGroup_Insurance_ChildInsurnaceId",
                table: "InsuranceGroup");

            migrationBuilder.DropForeignKey(
                name: "FK_InsuranceGroup_Insurance_ParentInsuranceId",
                table: "InsuranceGroup");

            migrationBuilder.DropForeignKey(
                name: "FK_NDCGroup_Medicine_ChildMedicineId",
                table: "NDCGroup");

            migrationBuilder.DropForeignKey(
                name: "FK_NDCGroup_Medicine_ParentMedicineId",
                table: "NDCGroup");

            migrationBuilder.DropTable(
                name: "Medicine");

            migrationBuilder.DropIndex(
                name: "IX_NDCGroup_ChildMedicineId",
                table: "NDCGroup");

            migrationBuilder.DropIndex(
                name: "IX_NDCGroup_ParentMedicineId",
                table: "NDCGroup");

            migrationBuilder.DropIndex(
                name: "IX_InsuranceGroup_ChildInsurnaceId",
                table: "InsuranceGroup");

            migrationBuilder.DropIndex(
                name: "IX_InsuranceGroup_ParentInsuranceId",
                table: "InsuranceGroup");

            migrationBuilder.DropColumn(
                name: "ChildMedicineId",
                table: "NDCGroup");

            migrationBuilder.DropColumn(
                name: "ParentMedicineId",
                table: "NDCGroup");

            migrationBuilder.DropColumn(
                name: "ChildInsurnaceId",
                table: "InsuranceGroup");

            migrationBuilder.DropColumn(
                name: "ParentInsuranceId",
                table: "InsuranceGroup");

            migrationBuilder.DropColumn(
                name: "Days",
                table: "Billing");

            migrationBuilder.DropColumn(
                name: "DrugForm",
                table: "Billing");

            migrationBuilder.DropColumn(
                name: "DrugName",
                table: "Billing");

            migrationBuilder.DropColumn(
                name: "DrugTherapy",
                table: "Billing");

            migrationBuilder.DropColumn(
                name: "FinalizedDate",
                table: "Billing");

            migrationBuilder.DropColumn(
                name: "InsName",
                table: "Billing");

            migrationBuilder.DropColumn(
                name: "PackageSize",
                table: "Billing");

            migrationBuilder.DropColumn(
                name: "Priins",
                table: "Billing");

            migrationBuilder.DropColumn(
                name: "PriinsBinNo",
                table: "Billing");

            migrationBuilder.DropColumn(
                name: "PriinsPaid",
                table: "Billing");

            migrationBuilder.DropColumn(
                name: "Quant",
                table: "Billing");

            migrationBuilder.DropColumn(
                name: "RXNO",
                table: "Billing");

            migrationBuilder.DropColumn(
                name: "Secins",
                table: "Billing");

            migrationBuilder.DropColumn(
                name: "SecinsBinNo",
                table: "Billing");

            migrationBuilder.DropColumn(
                name: "SecinsPaid",
                table: "Billing");

            migrationBuilder.DropColumn(
                name: "TotalInsPaid",
                table: "Billing");

            migrationBuilder.RenameColumn(
                name: "SourceFieldName",
                table: "BillingFieldMapping",
                newName: "SourceColumnName");

            migrationBuilder.RenameColumn(
                name: "DestinationFieldName",
                table: "BillingFieldMapping",
                newName: "CanonicalFieldName");

            migrationBuilder.AddColumn<string>(
                name: "GroupName",
                table: "NDCGroup",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Strength",
                table: "NDCGroup",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GroupCode",
                table: "InsuranceGroup",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "GroupName",
                table: "InsuranceGroup",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "InsuranceGroupId",
                table: "Insurance",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<Guid>(
                name: "InsuranceId",
                table: "Billing",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.CreateTable(
                name: "MedicineMaster",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NDCGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DrugName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Form = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NDC = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PacketSize = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Strength = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    StrengthType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Unit = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UnitType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
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

            migrationBuilder.CreateIndex(
                name: "IX_Insurance_InsuranceGroupId",
                table: "Insurance",
                column: "InsuranceGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicineMaster_NDCGroupId",
                table: "MedicineMaster",
                column: "NDCGroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_Billing_Insurance_InsuranceId",
                table: "Billing",
                column: "InsuranceId",
                principalTable: "Insurance",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Insurance_InsuranceGroup_InsuranceGroupId",
                table: "Insurance",
                column: "InsuranceGroupId",
                principalTable: "InsuranceGroup",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
