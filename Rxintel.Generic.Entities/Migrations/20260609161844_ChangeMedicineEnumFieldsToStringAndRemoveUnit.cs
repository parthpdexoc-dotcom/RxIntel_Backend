using System.ComponentModel;
using System.Reflection;
using System.Text;
using Microsoft.EntityFrameworkCore.Migrations;
using Rxintel.Generic.Enums;

#nullable disable

namespace Rxintel.Generic.Entities.Migrations
{
    /// <inheritdoc />
    public partial class ChangeMedicineEnumFieldsToStringAndRemoveUnit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Unit",
                table: "Medicine");

            ConvertEnumColumnToLabels<UnitStrength>(migrationBuilder, "StrengthUnit", 500);
            // Historical PackageSizeUnit labels (enum removed; kept local so fresh DBs can still apply this migration).
            ConvertEnumColumnToLabels<HistoricalPackageSizeUnit>(migrationBuilder, "PackageSizeUnit", 250);
            ConvertEnumColumnToLabels<MedicineForm>(migrationBuilder, "DosageForm", 250);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            RevertLabelsColumnToEnum<UnitStrength>(migrationBuilder, "StrengthUnit");
            RevertLabelsColumnToEnum<HistoricalPackageSizeUnit>(migrationBuilder, "PackageSizeUnit");
            RevertLabelsColumnToEnum<MedicineForm>(migrationBuilder, "DosageForm");

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "Medicine",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        private static void ConvertEnumColumnToLabels<TEnum>(
            MigrationBuilder migrationBuilder,
            string column,
            int maxLength)
            where TEnum : struct, Enum
        {
            var tempColumn = $"{column}Text";

            migrationBuilder.AddColumn<string>(
                name: tempColumn,
                table: "Medicine",
                type: $"nvarchar({maxLength})",
                maxLength: maxLength,
                nullable: true);

            migrationBuilder.Sql(BuildEnumIntToLabelUpdateSql<TEnum>(column, tempColumn));

            migrationBuilder.DropColumn(
                name: column,
                table: "Medicine");

            migrationBuilder.RenameColumn(
                name: tempColumn,
                table: "Medicine",
                newName: column);
        }

        private static void RevertLabelsColumnToEnum<TEnum>(
            MigrationBuilder migrationBuilder,
            string column)
            where TEnum : struct, Enum
        {
            var tempColumn = $"{column}Int";

            migrationBuilder.AddColumn<int>(
                name: tempColumn,
                table: "Medicine",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(BuildLabelToEnumIntUpdateSql<TEnum>(column, tempColumn));

            migrationBuilder.DropColumn(
                name: column,
                table: "Medicine");

            migrationBuilder.RenameColumn(
                name: tempColumn,
                table: "Medicine",
                newName: column);
        }

        private static string BuildEnumIntToLabelUpdateSql<TEnum>(string intColumn, string textColumn)
            where TEnum : struct, Enum
        {
            var sb = new StringBuilder();
            sb.Append($"UPDATE [Medicine] SET [{textColumn}] = CASE ");
            foreach (TEnum enumValue in Enum.GetValues<TEnum>())
            {
                var label = GetEnumDescription(enumValue);
                sb.Append($"WHEN [{intColumn}] = {Convert.ToInt32(enumValue)} THEN N'{EscapeSql(label)}' ");
            }
            sb.Append($"END WHERE [{intColumn}] IS NOT NULL");
            return sb.ToString();
        }

        private static string BuildLabelToEnumIntUpdateSql<TEnum>(string textColumn, string intColumn)
            where TEnum : struct, Enum
        {
            var sb = new StringBuilder();
            sb.Append($"UPDATE [Medicine] SET [{intColumn}] = CASE ");
            foreach (TEnum enumValue in Enum.GetValues<TEnum>())
            {
                var label = GetEnumDescription(enumValue);
                sb.Append($"WHEN [{textColumn}] = N'{EscapeSql(label)}' THEN {Convert.ToInt32(enumValue)} ");
            }
            sb.Append($"END WHERE [{textColumn}] IS NOT NULL");
            return sb.ToString();
        }

        private static string GetEnumDescription<TEnum>(TEnum enumValue)
            where TEnum : struct, Enum
        {
            var field = typeof(TEnum).GetField(enumValue.ToString()!);
            var description = field?.GetCustomAttribute<DescriptionAttribute>()?.Description;
            return string.IsNullOrWhiteSpace(description) ? enumValue.ToString()! : description;
        }

        private static string EscapeSql(string value) => value.Replace("'", "''");

        /// <summary>Local copy of removed UnitOfMeasure values for this historical migration only.</summary>
        private enum HistoricalPackageSizeUnit
        {
            [Description("min")] Min = 1,
            [Description("INSERT")] Insert = 2,
            [Description("OIL")] Oil = 3,
            [Description("DRUM")] Drum = 4,
            [Description("INHALANT")] Inhalant = 5,
            [Description("DISC")] Disc = 6,
            [Description("LOTION")] Lotion = 7,
            [Description("TROCHE")] Troche = 8,
            [Description("CELLULAR")] Cellular = 9,
            [Description("GAS")] Gas = 10,
            [Description("AEROSOL")] Aerosol = 11,
            [Description("AMPULE")] Ampule = 12,
            [Description("APPLICATOR")] Applicator = 13,
            [Description("BAG")] Bag = 14,
            [Description("BAR")] Bar = 15,
            [Description("BLISTER")] Blister = 16,
            [Description("BOTTLE")] Bottle = 17,
            [Description("BOX")] Box = 18,
            [Description("CAN")] Can = 19,
            [Description("CANISTER")] Canister = 20,
            [Description("CAPSULE")] Capsule = 21,
            [Description("CARTON")] Carton = 22,
            [Description("CARTRIDGE")] Cartridge = 23,
            [Description("CASE")] Case = 24,
            [Description("CELLO")] Cello = 25,
            [Description("CHEWABLE")] Chewable = 26,
            [Description("CLOTH")] Cloth = 27,
            [Description("cm")] Cm = 28,
            [Description("CONTAINER")] Container = 29,
            [Description("CREAM")] Cream = 30,
            [Description("CUP")] Cup = 31,
            [Description("CYLINDER")] Cylinder = 32,
            [Description("d")] D = 33,
            [Description("DOSE")] Dose = 34,
            [Description("FILM")] Film = 35,
            [Description("FOR")] For = 36,
            [Description("g")] G = 37,
            [Description("GEL")] Gel = 38,
            [Description("GRANULE")] Granule = 39,
            [Description("GUM")] Gum = 40,
            [Description("h")] H = 41,
            [Description("IMPLANT")] Implant = 42,
            [Description("INHALER")] Inhaler = 43,
            [Description("INJECTION")] Injection = 44,
            [Description("INTRAUTERINE")] Intrauterine = 45,
            [Description("JAR")] Jar = 46,
            [Description("JUG")] Jug = 47,
            [Description("kg")] Kg = 48,
            [Description("KIT")] Kit = 49,
            [Description("L")] L = 50,
            [Description("LIQUID")] Liquid = 51,
            [Description("LOZENGE")] Lozenge = 52,
            [Description("mg")] Mg = 53,
            [Description("mL")] Ml = 54,
            [Description("OINTMENT")] Ointment = 55,
            [Description("PACKAGE")] Package = 56,
            [Description("PACKET")] Packet = 57,
            [Description("PAIL")] Pail = 58,
            [Description("PASTE")] Paste = 59,
            [Description("PASTILLE")] Pastille = 60,
            [Description("PATCH")] Patch = 61,
            [Description("PELLET")] Pellet = 62,
            [Description("PLASTER")] Plaster = 63,
            [Description("POUCH")] Pouch = 64,
            [Description("POWDER")] Powder = 65,
            [Description("SOLUTION")] Solution = 66,
            [Description("SPRAY")] Spray = 67,
            [Description("STICK")] Stick = 68,
            [Description("STRIP")] Strip = 69,
            [Description("SUPPOSITORY")] Suppository = 70,
            [Description("SUSPENSION")] Suspension = 71,
            [Description("SWAB")] Swab = 72,
            [Description("SYRINGE")] Syringe = 73,
            [Description("SYRUP")] Syrup = 74,
            [Description("SYSTEM")] System = 75,
            [Description("TABLET")] Tablet = 76,
            [Description("TRAY")] Tray = 77,
            [Description("TUBE")] Tube = 78,
            [Description("U")] U = 79,
            [Description("VIAL")] Vial = 80
        }
    }
}
