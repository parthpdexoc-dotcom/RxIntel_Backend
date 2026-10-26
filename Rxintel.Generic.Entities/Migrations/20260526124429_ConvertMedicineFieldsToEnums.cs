using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rxintel.Generic.Entities.Migrations
{
    /// <inheritdoc />
    public partial class ConvertMedicineFieldsToEnums : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Strength",
                table: "Medicine",
                newName: "StrengthAmount");

            migrationBuilder.RenameColumn(
                name: "UnitOfMeasure",
                table: "Medicine",
                newName: "LegacyStrengthUnit");

            migrationBuilder.RenameColumn(
                name: "Form",
                table: "Medicine",
                newName: "LegacyForm");

            migrationBuilder.AddColumn<int>(
                name: "Strength",
                table: "Medicine",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UnitOfMeasure",
                table: "Medicine",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Form",
                table: "Medicine",
                type: "int",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE [Medicine]
                SET [Unit] = [StrengthAmount]
                WHERE [Unit] IS NULL
                  AND [StrengthAmount] IS NOT NULL
                  AND LTRIM(RTRIM([StrengthAmount])) <> ''
                  AND UPPER(LTRIM(RTRIM([StrengthAmount]))) NOT IN (
                    'MG','ML','MCG','GM','G','IU','MEQ','%','UNIT','UNIT/ML',
                    'MG/5ML','MCG/ML','MG/ML','MG/10ML','IU/ML'
                  )
                """);

            migrationBuilder.Sql("""
                UPDATE [Medicine]
                SET [Strength] = CASE UPPER(LTRIM(RTRIM([LegacyStrengthUnit])))
                    WHEN 'MG' THEN 1
                    WHEN 'ML' THEN 2
                    WHEN 'MCG' THEN 3
                    WHEN 'GM' THEN 4
                    WHEN 'G' THEN 5
                    WHEN 'IU' THEN 6
                    WHEN 'MEQ' THEN 7
                    WHEN '%' THEN 8
                    WHEN 'UNIT' THEN 9
                    WHEN 'UNIT/ML' THEN 10
                    WHEN 'MG/5ML' THEN 11
                    WHEN 'MCG/ML' THEN 12
                    WHEN 'MG/ML' THEN 13
                    WHEN 'MG/10ML' THEN 14
                    WHEN 'IU/ML' THEN 15
                    ELSE NULL
                END
                WHERE [LegacyStrengthUnit] IS NOT NULL AND LTRIM(RTRIM([LegacyStrengthUnit])) <> ''
                """);

            migrationBuilder.Sql("""
                UPDATE [Medicine]
                SET [UnitOfMeasure] = CASE UPPER(LTRIM(RTRIM([UnitType])))
                    WHEN 'EACH' THEN 1
                    WHEN 'ML' THEN 2
                    WHEN 'BOTTLE' THEN 3
                    WHEN 'TUBE' THEN 4
                    WHEN 'VIAL' THEN 5
                    WHEN 'PACKET' THEN 6
                    WHEN 'KIT' THEN 7
                    WHEN 'BOX' THEN 8
                    WHEN 'CARTON' THEN 9
                    WHEN 'CASE' THEN 10
                    WHEN 'PACK' THEN 11
                    ELSE NULL
                END
                WHERE [UnitType] IS NOT NULL AND LTRIM(RTRIM([UnitType])) <> ''
                """);

            migrationBuilder.Sql("""
                UPDATE [Medicine]
                SET [Form] = CASE UPPER(LTRIM(RTRIM([LegacyForm])))
                    WHEN 'TABLET' THEN 1
                    WHEN 'CAPSULE' THEN 2
                    WHEN 'SYRUP' THEN 3
                    WHEN 'INJECTION' THEN 4
                    WHEN 'CREAM' THEN 5
                    WHEN 'OINTMENT' THEN 6
                    WHEN 'GEL' THEN 7
                    WHEN 'EYE DROPS' THEN 8
                    WHEN 'NASAL SPRAY' THEN 9
                    WHEN 'INHALER' THEN 10
                    WHEN 'POWDER' THEN 11
                    WHEN 'SUSPENSION' THEN 12
                    WHEN 'SOLUTION' THEN 13
                    WHEN 'LOTION' THEN 14
                    WHEN 'PATCH' THEN 15
                    WHEN 'SUPPOSITORY' THEN 16
                    WHEN 'DROPS' THEN 17
                    WHEN 'SPRAY' THEN 18
                    ELSE NULL
                END
                WHERE [LegacyForm] IS NOT NULL AND LTRIM(RTRIM([LegacyForm])) <> ''
                """);

            migrationBuilder.DropColumn(
                name: "StrengthAmount",
                table: "Medicine");

            migrationBuilder.DropColumn(
                name: "LegacyStrengthUnit",
                table: "Medicine");

            migrationBuilder.DropColumn(
                name: "LegacyForm",
                table: "Medicine");

            migrationBuilder.DropColumn(
                name: "UnitType",
                table: "Medicine");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UnitType",
                table: "Medicine",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LegacyForm",
                table: "Medicine",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LegacyStrengthUnit",
                table: "Medicine",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StrengthAmount",
                table: "Medicine",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE [Medicine]
                SET [LegacyStrengthUnit] = CASE [Strength]
                    WHEN 1 THEN 'MG'
                    WHEN 2 THEN 'ML'
                    WHEN 3 THEN 'MCG'
                    WHEN 4 THEN 'GM'
                    WHEN 5 THEN 'G'
                    WHEN 6 THEN 'IU'
                    WHEN 7 THEN 'MEQ'
                    WHEN 8 THEN '%'
                    WHEN 9 THEN 'UNIT'
                    WHEN 10 THEN 'UNIT/ML'
                    WHEN 11 THEN 'MG/5ML'
                    WHEN 12 THEN 'MCG/ML'
                    WHEN 13 THEN 'MG/ML'
                    WHEN 14 THEN 'MG/10ML'
                    WHEN 15 THEN 'IU/ML'
                    ELSE NULL
                END
                """);

            migrationBuilder.Sql("""
                UPDATE [Medicine]
                SET [UnitType] = CASE [UnitOfMeasure]
                    WHEN 1 THEN 'Each'
                    WHEN 2 THEN 'ML'
                    WHEN 3 THEN 'Bottle'
                    WHEN 4 THEN 'Tube'
                    WHEN 5 THEN 'Vial'
                    WHEN 6 THEN 'Packet'
                    WHEN 7 THEN 'Kit'
                    WHEN 8 THEN 'Box'
                    WHEN 9 THEN 'Carton'
                    WHEN 10 THEN 'Case'
                    WHEN 11 THEN 'Pack'
                    ELSE NULL
                END
                """);

            migrationBuilder.Sql("""
                UPDATE [Medicine]
                SET [LegacyForm] = CASE [Form]
                    WHEN 1 THEN 'Tablet'
                    WHEN 2 THEN 'Capsule'
                    WHEN 3 THEN 'Syrup'
                    WHEN 4 THEN 'Injection'
                    WHEN 5 THEN 'Cream'
                    WHEN 6 THEN 'Ointment'
                    WHEN 7 THEN 'Gel'
                    WHEN 8 THEN 'Eye Drops'
                    WHEN 9 THEN 'Nasal Spray'
                    WHEN 10 THEN 'Inhaler'
                    WHEN 11 THEN 'Powder'
                    WHEN 12 THEN 'Suspension'
                    WHEN 13 THEN 'Solution'
                    WHEN 14 THEN 'Lotion'
                    WHEN 15 THEN 'Patch'
                    WHEN 16 THEN 'Suppository'
                    WHEN 17 THEN 'Drops'
                    WHEN 18 THEN 'Spray'
                    ELSE NULL
                END
                """);

            migrationBuilder.Sql("""
                UPDATE [Medicine]
                SET [StrengthAmount] = [Unit]
                WHERE [Unit] IS NOT NULL
                """);

            migrationBuilder.DropColumn(
                name: "Form",
                table: "Medicine");

            migrationBuilder.DropColumn(
                name: "UnitOfMeasure",
                table: "Medicine");

            migrationBuilder.DropColumn(
                name: "Strength",
                table: "Medicine");

            migrationBuilder.RenameColumn(
                name: "LegacyForm",
                table: "Medicine",
                newName: "Form");

            migrationBuilder.RenameColumn(
                name: "LegacyStrengthUnit",
                table: "Medicine",
                newName: "UnitOfMeasure");

            migrationBuilder.RenameColumn(
                name: "StrengthAmount",
                table: "Medicine",
                newName: "Strength");
        }
    }
}
