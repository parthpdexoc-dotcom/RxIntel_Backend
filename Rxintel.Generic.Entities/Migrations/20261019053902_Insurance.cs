using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rxintel.Generic.Entities.Migrations
{
    /// <inheritdoc />
    public partial class Insurance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BinNo",
                table: "ErxAuditRecord",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InsCode",
                table: "ErxAuditRecord",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "ErxAuditRecord",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalInsPaid",
                table: "ErxAuditRecord",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BinNo",
                table: "ErxAuditRecord");

            migrationBuilder.DropColumn(
                name: "InsCode",
                table: "ErxAuditRecord");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "ErxAuditRecord");

            migrationBuilder.DropColumn(
                name: "TotalInsPaid",
                table: "ErxAuditRecord");
        }
    }
}
