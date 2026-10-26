using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rxintel.Generic.Entities.Migrations
{
    /// <inheritdoc />
    public partial class AddSecondaryInsuranceIdToBilling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SecondaryInsuranceId",
                table: "Billing",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Billing_SecondaryInsuranceId",
                table: "Billing",
                column: "SecondaryInsuranceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Billing_Insurance_SecondaryInsuranceId",
                table: "Billing",
                column: "SecondaryInsuranceId",
                principalTable: "Insurance",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Billing_Insurance_SecondaryInsuranceId",
                table: "Billing");

            migrationBuilder.DropIndex(
                name: "IX_Billing_SecondaryInsuranceId",
                table: "Billing");

            migrationBuilder.DropColumn(
                name: "SecondaryInsuranceId",
                table: "Billing");
        }
    }
}
