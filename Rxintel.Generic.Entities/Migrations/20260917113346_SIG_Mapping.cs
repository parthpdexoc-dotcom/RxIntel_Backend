using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rxintel.Generic.Entities.Migrations
{
    /// <inheritdoc />
    public partial class SIG_Mapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ErxSigMapping",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Abbreviation = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SimpleEnglish = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    NormalizedToken = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ClinicalContext = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErxSigMapping", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ErxSigMapping_Abbreviation",
                table: "ErxSigMapping",
                column: "Abbreviation");

            migrationBuilder.CreateIndex(
                name: "IX_ErxSigMapping_Category",
                table: "ErxSigMapping",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_ErxSigMapping_IsActive",
                table: "ErxSigMapping",
                column: "IsActive");

            migrationBuilder.InsertData(
                table: "ErxSigMapping",
                columns: new[] { "Id", "Abbreviation", "SimpleEnglish", "NormalizedToken", "Category", "ClinicalContext", "IsActive", "CreatedDate" },
                values: new object[,]
                {
                    { 1, "PO", "By mouth (orally)", "by mouth", "General", null, true, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, "SL", "Under the tongue (sublingually)", "sublingually", "General", null, true, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, "PR", "In the rectum (rectally)", "rectally", "General", null, true, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { 4, "PV", "In the vagina (vaginally)", "vaginally", "General", null, true, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { 5, "IM", "Into the muscle (intramuscularly)", "intramuscularly", "General", null, true, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { 6, "IV", "Into the vein (intravenously)", "intravenously", "General", null, true, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { 7, "SC / SQ", "Under the skin (subcutaneously)", "subcutaneously", "General", null, true, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { 8, "TOP", "Topically (apply to skin)", "topically", "General", null, true, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { 9, "INH", "Inhale (by inhalation)", "by inhalation", "General", null, true, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { 10, "OD", "Right eye", "right eye", "General", null, true, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { 11, "OS", "Left eye", "left eye", "General", null, true, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { 12, "OU", "Both eyes", "both eyes", "General", null, true, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { 13, "AD", "Right ear", "right ear", "General", null, true, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { 14, "AS", "Left ear", "left ear", "General", null, true, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { 15, "AU", "Both ears", "both ears", "General", null, true, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { 16, "QD / daily", "Once a day", "daily", "General", null, true, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { 17, "BID", "Twice a day", "twice daily", "General", null, true, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { 18, "TID", "Three times a day", "three times a day", "General", null, true, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { 19, "QID", "Four times a day", "four times a day", "General", null, true, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { 20, "QHS", "Every night at bedtime", "at bedtime", "General", null, true, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { 21, "QAM", "Every morning", "every morning", "General", null, true, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { 22, "QPM", "Every evening", "every evening", "General", null, true, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { 23, "Q4H", "Every 4 hours", "every 4 hours", "General", null, true, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { 24, "Q6H", "Every 6 hours", "every 6 hours", "General", null, true, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { 25, "Q8H", "Every 8 hours", "every 8 hours", "General", null, true, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { 26, "Q12H", "Every 12 hours", "every 12 hours", "General", null, true, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { 27, "QOD", "Every other day", "every other day", "General", null, true, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { 28, "PRN", "As needed", "as needed", "General", null, true, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { 29, "STAT", "Immediately", "immediately", "General", null, true, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { 30, "TAB", "Tablet", "tablet", "General", null, true, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { 31, "CAP", "Capsule", "capsule", "General", null, true, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { 32, "GTT / GTTS", "Drop / Drops", "drops", "General", null, true, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { 33, "LIQ", "Liquid", "liquid", "General", null, true, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { 34, "SOLN", "Solution", "solution", "General", null, true, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { 35, "SUSP", "Suspension", "suspension", "General", null, true, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { 36, "UNG / OINT", "Ointment", "ointment", "General", null, true, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { 37, "CRM", "Cream", "cream", "General", null, true, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { 38, "SUPP", "Suppository", "suppository", "General", null, true, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { 39, "Qty_ORD", "Quantity Ordered", "qty ord", "General", null, true, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { 40, "Qty_DSP", "Quantity Dispensed", "qty dsp", "General", null, true, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { 41, "Qty_AUTH", "Quantity Authorized", "qty auth", "General", null, true, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { 42, "AC", "Before meals", "before meals", "General", null, true, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { 43, "PC", "After meals", "after meals", "General", null, true, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { 44, "C", "With", "with", "General", null, true, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { 45, "S", "Without", "without", "General", null, true, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { 46, "UD / AAA", "As directed / Apply to affected area", "as directed", "General", null, true, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ErxSigMapping");
        }
    }
}
