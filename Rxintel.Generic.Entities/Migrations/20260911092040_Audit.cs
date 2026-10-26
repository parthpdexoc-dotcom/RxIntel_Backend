using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Rxintel.Generic.Entities.Migrations
{
    /// <inheritdoc />
    public partial class Audit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ErxAuditBatch",
                columns: table => new
                {
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    UploadedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalRows = table.Column<int>(type: "int", nullable: false),
                    TotalDiscrepancies = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErxAuditBatch", x => x.BatchId);
                });

            migrationBuilder.CreateTable(
                name: "InsuranceCompany",
                columns: table => new
                {
                    InsuranceCompanyId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InsuranceCompany", x => x.InsuranceCompanyId);
                });

            migrationBuilder.CreateTable(
                name: "ErxAuditRecord",
                columns: table => new
                {
                    RecordId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RxNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MmFillNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ErxRxNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MmOrderDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ErxOrderDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ErxEffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MmDateFilled = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MmTimeFilled = table.Column<TimeSpan>(type: "time", nullable: true),
                    PickupDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ErxReceivedDateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MmPatientName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ErxPatientName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MmPatientDob = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ErxPatientDob = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MmPatientPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ErxPatientPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    MmPatientAddress = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ErxPatientAddress = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    MmPrescriberName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ErxPrescriberName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SupervisorName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MmPrescriberNpi = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ErxPrescriberNpi = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    MmPrescriberDea = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ErxPrescriberDea = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    MmDispensedDrugName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ErxDrugName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    MmPotencyUnitCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ErxPotencyUnitCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    MmDispensedNdc = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ErxNdc = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    MmQtyOrd = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    ErxQtyOrd = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    MmQtyDispensed = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    ErxQtyDispensed = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    MmDispensedDaysSupply = table.Column<int>(type: "int", nullable: true),
                    ErxDaysSupply = table.Column<int>(type: "int", nullable: true),
                    MmDispensedSig = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ErxSig = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TelephoneNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TelephoneNotesDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ErxNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErxNotesDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ErxNotesCreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MmDawCode = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    ErxDawCode = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    MmRefillsPharmacistEntered = table.Column<int>(type: "int", nullable: true),
                    MmRphNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MmSignatureCaptured = table.Column<bool>(type: "bit", nullable: true),
                    FollowUpTag = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MmDelivery = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    MmRxOrigin = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    MmCopay = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    DurReasonCd = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    DurProfCd = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    DurResultCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErxAuditRecord", x => x.RecordId);
                    table.ForeignKey(
                        name: "FK_ErxAuditRecord_ErxAuditBatch_BatchId",
                        column: x => x.BatchId,
                        principalTable: "ErxAuditBatch",
                        principalColumn: "BatchId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ErxColumnMatchRule",
                columns: table => new
                {
                    RuleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FieldGroupName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MmColumnName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ErxColumnName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MatchType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ToleranceValue = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    InsuranceCompanyId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErxColumnMatchRule", x => x.RuleId);
                    table.ForeignKey(
                        name: "FK_ErxColumnMatchRule_InsuranceCompany_InsuranceCompanyId",
                        column: x => x.InsuranceCompanyId,
                        principalTable: "InsuranceCompany",
                        principalColumn: "InsuranceCompanyId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ErxAuditAlertLog",
                columns: table => new
                {
                    AlertLogId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecordId = table.Column<long>(type: "bigint", nullable: false),
                    AlertMessage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AlertType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErxAuditAlertLog", x => x.AlertLogId);
                    table.ForeignKey(
                        name: "FK_ErxAuditAlertLog_ErxAuditRecord_RecordId",
                        column: x => x.RecordId,
                        principalTable: "ErxAuditRecord",
                        principalColumn: "RecordId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ErxAuditDiscrepancy",
                columns: table => new
                {
                    DiscrepancyId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecordId = table.Column<long>(type: "bigint", nullable: false),
                    RuleId = table.Column<int>(type: "int", nullable: false),
                    MmValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ErxValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ReviewedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErxAuditDiscrepancy", x => x.DiscrepancyId);
                    table.ForeignKey(
                        name: "FK_ErxAuditDiscrepancy_ErxAuditRecord_RecordId",
                        column: x => x.RecordId,
                        principalTable: "ErxAuditRecord",
                        principalColumn: "RecordId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ErxAuditDiscrepancy_ErxColumnMatchRule_RuleId",
                        column: x => x.RuleId,
                        principalTable: "ErxColumnMatchRule",
                        principalColumn: "RuleId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "ErxColumnMatchRule",
                columns: new[] { "RuleId", "CreatedDate", "ErxColumnName", "FieldGroupName", "InsuranceCompanyId", "IsActive", "MatchType", "MmColumnName", "ToleranceValue" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "ErxRxNumber", "RX Number", null, true, "Exact", "RxNumber", null },
                    { 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "ErxDrugName", "Drug Name", null, true, "Fuzzy", "MmDispensedDrugName", null },
                    { 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "ErxNdc", "NDC", null, true, "Exact", "MmDispensedNdc", null },
                    { 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "ErxQtyOrd", "Quantity Ordered", null, true, "NumericTolerance", "MmQtyOrd", 0.00m },
                    { 5, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "ErxQtyDispensed", "Quantity Dispensed", null, true, "NumericTolerance", "MmQtyDispensed", 0.00m },
                    { 6, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "ErxDaysSupply", "Days Supply", null, true, "NumericTolerance", "MmDispensedDaysSupply", 0.00m },
                    { 7, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "ErxPrescriberNpi", "Prescriber NPI", null, true, "Exact", "MmPrescriberNpi", null },
                    { 8, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "ErxPrescriberDea", "Prescriber DEA", null, true, "CaseInsensitive", "MmPrescriberDea", null },
                    { 9, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "ErxPatientName", "Patient Name", null, true, "CaseInsensitive", "MmPatientName", null },
                    { 10, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "ErxPatientDob", "Patient DOB", null, true, "Exact", "MmPatientDob", null },
                    { 11, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "ErxDawCode", "DAW Code", null, true, "Exact", "MmDawCode", null },
                    { 12, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "ErxSig", "Sig / Directions", null, true, "CaseInsensitive", "MmDispensedSig", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ErxAuditAlertLog_RecordId",
                table: "ErxAuditAlertLog",
                column: "RecordId");

            migrationBuilder.CreateIndex(
                name: "IX_ErxAuditBatch_UploadedAt",
                table: "ErxAuditBatch",
                column: "UploadedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ErxAuditDiscrepancy_RecordId",
                table: "ErxAuditDiscrepancy",
                column: "RecordId");

            migrationBuilder.CreateIndex(
                name: "IX_ErxAuditDiscrepancy_RuleId",
                table: "ErxAuditDiscrepancy",
                column: "RuleId");

            migrationBuilder.CreateIndex(
                name: "IX_ErxAuditDiscrepancy_Status",
                table: "ErxAuditDiscrepancy",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ErxAuditRecord_BatchId",
                table: "ErxAuditRecord",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_ErxAuditRecord_RxNumber",
                table: "ErxAuditRecord",
                column: "RxNumber");

            migrationBuilder.CreateIndex(
                name: "IX_ErxColumnMatchRule_InsuranceCompanyId",
                table: "ErxColumnMatchRule",
                column: "InsuranceCompanyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ErxAuditAlertLog");

            migrationBuilder.DropTable(
                name: "ErxAuditDiscrepancy");

            migrationBuilder.DropTable(
                name: "ErxAuditRecord");

            migrationBuilder.DropTable(
                name: "ErxColumnMatchRule");

            migrationBuilder.DropTable(
                name: "ErxAuditBatch");

            migrationBuilder.DropTable(
                name: "InsuranceCompany");
        }
    }
}
