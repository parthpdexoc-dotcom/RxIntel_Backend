using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rxintel.Generic.Entities.Migrations
{
    /// <inheritdoc />
    public partial class AddStateTableAndPharmacyStateId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StateCode",
                table: "Pharmacy");

            migrationBuilder.RenameColumn(
                name: "DestinationField",
                table: "WholesalerFieldMapping",
                newName: "DestinationColumnName");

            migrationBuilder.AlterColumn<string>(
                name: "SourceColumnName",
                table: "WholesalerFieldMapping",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "StateId",
                table: "Pharmacy",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "State",
                columns: table => new
                {
                    StateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StateName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_State", x => x.StateId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Pharmacy_StateId",
                table: "Pharmacy",
                column: "StateId");

            migrationBuilder.AddForeignKey(
                name: "FK_Pharmacy_State_StateId",
                table: "Pharmacy",
                column: "StateId",
                principalTable: "State",
                principalColumn: "StateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Pharmacy_State_StateId",
                table: "Pharmacy");

            migrationBuilder.DropTable(
                name: "State");

            migrationBuilder.DropIndex(
                name: "IX_Pharmacy_StateId",
                table: "Pharmacy");

            migrationBuilder.DropColumn(
                name: "StateId",
                table: "Pharmacy");

            migrationBuilder.RenameColumn(
                name: "DestinationColumnName",
                table: "WholesalerFieldMapping",
                newName: "DestinationField");

            migrationBuilder.AlterColumn<string>(
                name: "SourceColumnName",
                table: "WholesalerFieldMapping",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<string>(
                name: "StateCode",
                table: "Pharmacy",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }
    }
}
