using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rxintel.Generic.Entities.Migrations
{
    /// <inheritdoc />
    public partial class RenameBillingEntityColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TotalInsPaid",
                table: "Billing",
                newName: "TotalInsurancePaidAmount");

            migrationBuilder.RenameColumn(
                name: "SecinsPaid",
                table: "Billing",
                newName: "SecondaryInsurancePaidAmount");

            migrationBuilder.RenameColumn(
                name: "SecinsBinNo",
                table: "Billing",
                newName: "SecondaryInsuranceBinNo");

            migrationBuilder.RenameColumn(
                name: "Secins",
                table: "Billing",
                newName: "SecondaryInsurance");

            migrationBuilder.RenameColumn(
                name: "Quant",
                table: "Billing",
                newName: "BilledQuantity");

            migrationBuilder.RenameColumn(
                name: "PriinsPaid",
                table: "Billing",
                newName: "PrimaryInsurancePaidAmount");

            migrationBuilder.RenameColumn(
                name: "PriinsBinNo",
                table: "Billing",
                newName: "PrimaryInsuranceBinNo");

            migrationBuilder.RenameColumn(
                name: "Priins",
                table: "Billing",
                newName: "PrimaryInsurance");

            migrationBuilder.RenameColumn(
                name: "InsName",
                table: "Billing",
                newName: "InsuranceName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TotalInsurancePaidAmount",
                table: "Billing",
                newName: "TotalInsPaid");

            migrationBuilder.RenameColumn(
                name: "SecondaryInsurancePaidAmount",
                table: "Billing",
                newName: "SecinsPaid");

            migrationBuilder.RenameColumn(
                name: "SecondaryInsuranceBinNo",
                table: "Billing",
                newName: "SecinsBinNo");

            migrationBuilder.RenameColumn(
                name: "SecondaryInsurance",
                table: "Billing",
                newName: "Secins");

            migrationBuilder.RenameColumn(
                name: "PrimaryInsurancePaidAmount",
                table: "Billing",
                newName: "PriinsPaid");

            migrationBuilder.RenameColumn(
                name: "PrimaryInsuranceBinNo",
                table: "Billing",
                newName: "PriinsBinNo");

            migrationBuilder.RenameColumn(
                name: "PrimaryInsurance",
                table: "Billing",
                newName: "Priins");

            migrationBuilder.RenameColumn(
                name: "InsuranceName",
                table: "Billing",
                newName: "InsName");

            migrationBuilder.RenameColumn(
                name: "BilledQuantity",
                table: "Billing",
                newName: "Quant");
        }
    }
}
