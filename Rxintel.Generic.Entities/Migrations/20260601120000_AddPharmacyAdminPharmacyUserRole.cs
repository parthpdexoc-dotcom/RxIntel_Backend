using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rxintel.Generic.Entities.Migrations
{
    /// <inheritdoc />
    public partial class AddPharmacyAdminPharmacyUserRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE [PharmacyUser] SET [Role] = 5 WHERE [Role] = 4;
                UPDATE [PharmacyUser] SET [Role] = 4 WHERE [Role] = 3;
                UPDATE [PharmacyUser] SET [Role] = 3 WHERE [Role] = 2;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE [PharmacyUser] SET [Role] = 2 WHERE [Role] = 3;
                UPDATE [PharmacyUser] SET [Role] = 3 WHERE [Role] = 4;
                UPDATE [PharmacyUser] SET [Role] = 4 WHERE [Role] = 5;
                """);
        }
    }
}
