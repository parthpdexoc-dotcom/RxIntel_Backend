using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rxintel.Generic.Entities.Migrations
{
    /// <inheritdoc />
    public partial class AddStateBoardLicenseRuleIdToState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StateBoardLicenseRuleId",
                table: "State",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "NUM_5_10");

            migrationBuilder.Sql(
                """
                UPDATE State SET StateBoardLicenseRuleId = 'NUM_5_6'
                WHERE UPPER(LTRIM(RTRIM(ISNULL(StateCode, '')))) = 'TX' OR StateName = 'Texas';

                UPDATE State SET StateBoardLicenseRuleId = 'NUM_6'
                WHERE UPPER(LTRIM(RTRIM(ISNULL(StateCode, '')))) = 'NY' OR StateName = 'New York';

                UPDATE State SET StateBoardLicenseRuleId = 'NUM_8_10'
                WHERE UPPER(LTRIM(RTRIM(ISNULL(StateCode, '')))) = 'OH' OR StateName = 'Ohio';

                UPDATE State SET StateBoardLicenseRuleId = 'PREFIX_2L_D'
                WHERE UPPER(LTRIM(RTRIM(ISNULL(StateCode, '')))) = 'FL' OR StateName = 'Florida';

                UPDATE State SET StateBoardLicenseRuleId = 'ALNUM_ANY'
                WHERE UPPER(LTRIM(RTRIM(ISNULL(StateCode, '')))) IN ('AK', 'WA')
                   OR StateName IN ('Alaska', 'Washington');

                UPDATE State SET StateBoardLicenseRuleId = 'ALNUM_BOTH'
                WHERE UPPER(LTRIM(RTRIM(ISNULL(StateCode, '')))) = 'LA' OR StateName = 'Louisiana';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StateBoardLicenseRuleId",
                table: "State");
        }
    }
}
