using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rxintel.Generic.Entities.Migrations
{
    /// <inheritdoc />
    public partial class ConvertPharmacyUserRoleToEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UserName",
                table: "PharmacyUser",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "FullName",
                table: "PharmacyUser",
                newName: "Name");

            migrationBuilder.AddColumn<string>(
                name: "Password",
                table: "PharmacyUser",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Role",
                table: "PharmacyUser",
                type: "int",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE [PharmacyUser]
                SET [Role] = CASE UPPER(LTRIM(RTRIM([RoleName])))
                    WHEN 'MANAGER' THEN 1
                    WHEN 'PHARMACIST' THEN 2
                    WHEN 'PHARMACY TECH' THEN 3
                    WHEN 'PHARMACYTECH' THEN 3
                    ELSE 1
                END
                """);

            migrationBuilder.DropColumn(
                name: "RoleName",
                table: "PharmacyUser");

            migrationBuilder.AlterColumn<int>(
                name: "Role",
                table: "PharmacyUser",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RoleName",
                table: "PharmacyUser",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE [PharmacyUser]
                SET [RoleName] = CASE [Role]
                    WHEN 1 THEN 'Manager'
                    WHEN 2 THEN 'Pharmacist'
                    WHEN 3 THEN 'Pharmacy Tech'
                    ELSE 'Manager'
                END
                """);

            migrationBuilder.DropColumn(
                name: "Role",
                table: "PharmacyUser");

            migrationBuilder.AlterColumn<string>(
                name: "RoleName",
                table: "PharmacyUser",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "Password",
                table: "PharmacyUser");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "PharmacyUser",
                newName: "UserName");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "PharmacyUser",
                newName: "FullName");
        }
    }
}
