using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rxintel.Generic.Entities.Migrations
{
    /// <inheritdoc />
    public partial class added_ins_columns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "MatchType",
                table: "ErxColumnMatchRule",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<string>(
                name: "InsuranceCompanyIds",
                table: "ErxColumnMatchRule",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InsuranceCompanyNames",
                table: "ErxColumnMatchRule",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "ErxColumnMatchRule",
                keyColumn: "RuleId",
                keyValue: 1,
                columns: new[] { "InsuranceCompanyIds", "InsuranceCompanyNames" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "ErxColumnMatchRule",
                keyColumn: "RuleId",
                keyValue: 2,
                columns: new[] { "InsuranceCompanyIds", "InsuranceCompanyNames" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "ErxColumnMatchRule",
                keyColumn: "RuleId",
                keyValue: 3,
                columns: new[] { "InsuranceCompanyIds", "InsuranceCompanyNames" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "ErxColumnMatchRule",
                keyColumn: "RuleId",
                keyValue: 4,
                columns: new[] { "InsuranceCompanyIds", "InsuranceCompanyNames" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "ErxColumnMatchRule",
                keyColumn: "RuleId",
                keyValue: 5,
                columns: new[] { "InsuranceCompanyIds", "InsuranceCompanyNames" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "ErxColumnMatchRule",
                keyColumn: "RuleId",
                keyValue: 6,
                columns: new[] { "InsuranceCompanyIds", "InsuranceCompanyNames" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "ErxColumnMatchRule",
                keyColumn: "RuleId",
                keyValue: 7,
                columns: new[] { "InsuranceCompanyIds", "InsuranceCompanyNames" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "ErxColumnMatchRule",
                keyColumn: "RuleId",
                keyValue: 8,
                columns: new[] { "InsuranceCompanyIds", "InsuranceCompanyNames" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "ErxColumnMatchRule",
                keyColumn: "RuleId",
                keyValue: 9,
                columns: new[] { "InsuranceCompanyIds", "InsuranceCompanyNames" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "ErxColumnMatchRule",
                keyColumn: "RuleId",
                keyValue: 10,
                columns: new[] { "InsuranceCompanyIds", "InsuranceCompanyNames" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "ErxColumnMatchRule",
                keyColumn: "RuleId",
                keyValue: 11,
                columns: new[] { "InsuranceCompanyIds", "InsuranceCompanyNames" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "ErxColumnMatchRule",
                keyColumn: "RuleId",
                keyValue: 12,
                columns: new[] { "InsuranceCompanyIds", "InsuranceCompanyNames" },
                values: new object[] { null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InsuranceCompanyIds",
                table: "ErxColumnMatchRule");

            migrationBuilder.DropColumn(
                name: "InsuranceCompanyNames",
                table: "ErxColumnMatchRule");

            migrationBuilder.AlterColumn<string>(
                name: "MatchType",
                table: "ErxColumnMatchRule",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);
        }
    }
}
