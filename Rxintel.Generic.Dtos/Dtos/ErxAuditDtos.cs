using System;
using System.Collections.Generic;

namespace Rxintel.Generic.Dtos.Dtos
{
    public class ErxAuditBatchDto
    {
        public Guid BatchId { get; set; }
        public Guid? PharmacyId { get; set; }
        public string? PharmacyName { get; set; }
        public string? PharmacyNpi { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string? UploadedBy { get; set; }
        public DateTime UploadedAt { get; set; }
        public int TotalRows { get; set; }
        public int TotalDiscrepancies { get; set; }
        public string Status { get; set; } = "Completed";
        public DateTime CreatedDate { get; set; }
    }

    public class ErxAuditRecordDto
    {
        public long RecordId { get; set; }
        public Guid BatchId { get; set; }
        public Guid? PharmacyId { get; set; }
        public string? PharmacyName { get; set; }
        public string RxNumber { get; set; } = string.Empty;
        public string? MmFillNumber { get; set; }
        public string? ErxRxNumber { get; set; }

        public DateTime? MmOrderDate { get; set; }
        public DateTime? ErxOrderDate { get; set; }
        public DateTime? ErxEffectiveDate { get; set; }
        public DateTime? MmDateFilled { get; set; }
        public TimeSpan? MmTimeFilled { get; set; }
        public DateTime? PickupDate { get; set; }
        public DateTime? ErxReceivedDateTime { get; set; }

        public string? PaymentMethod { get; set; }
        public string? InsCode { get; set; }
        public string? BinNo { get; set; }
        public decimal? TotalInsPaid { get; set; }

        public string? MmPatientName { get; set; }
        public string? ErxPatientName { get; set; }
        public DateTime? MmPatientDob { get; set; }
        public DateTime? ErxPatientDob { get; set; }
        public string? MmPatientPhone { get; set; }
        public string? ErxPatientPhone { get; set; }
        public string? MmPatientAddress { get; set; }
        public string? ErxPatientAddress { get; set; }

        public string? MmPrescriberName { get; set; }
        public string? ErxPrescriberName { get; set; }
        public string? SupervisorName { get; set; }
        public string? MmPrescriberNpi { get; set; }
        public string? ErxPrescriberNpi { get; set; }
        public string? MmPrescriberDea { get; set; }
        public string? ErxPrescriberDea { get; set; }

        public string? MmDispensedDrugName { get; set; }
        public string? ErxDrugName { get; set; }
        public string? MmPotencyUnitCode { get; set; }
        public string? ErxPotencyUnitCode { get; set; }
        public string? MmDispensedNdc { get; set; }
        public string? ErxNdc { get; set; }

        public decimal? MmQtyOrd { get; set; }
        public decimal? ErxQtyOrd { get; set; }
        public decimal? MmQtyDispensed { get; set; }
        public decimal? ErxQtyDispensed { get; set; }
        public int? MmDispensedDaysSupply { get; set; }
        public int? ErxDaysSupply { get; set; }
        public int? ErxBasedDaysSupply { get; set; }

        public string? MmDispensedSig { get; set; }
        public string? ErxSig { get; set; }

        public string? TelephoneNotes { get; set; }
        public DateTime? TelephoneNotesDate { get; set; }
        public string? ErxNotes { get; set; }
        public DateTime? ErxNotesDate { get; set; }
        public string? ErxNotesCreatedBy { get; set; }

        public string? MmDawCode { get; set; }
        public string? ErxDawCode { get; set; }
        public int? MmRefillsPharmacistEntered { get; set; }
        public int? ErxRefill { get; set; }
        public string? MmRphNotes { get; set; }
        public bool? MmSignatureCaptured { get; set; }
        public string? FollowUpTag { get; set; }
        public string? MmDelivery { get; set; }
        public string? MmRxOrigin { get; set; }
        public decimal? MmCopay { get; set; }
        public string? DurReasonCd { get; set; }
        public string? DurProfCd { get; set; }
        public string? DurResultCode { get; set; }
        public string? Message { get; set; }
        public DateTime CreatedAt { get; set; }

        public int DiscrepancyCount { get; set; }
        public bool HasDiscrepancy => DiscrepancyCount > 0;
        public List<ErxAuditDiscrepancyDto> Discrepancies { get; set; } = new();

        public bool IsInsurancePriority => !string.IsNullOrWhiteSpace(PaymentMethod) &&
            (PaymentMethod.Trim().Equals("I", StringComparison.OrdinalIgnoreCase) ||
             PaymentMethod.Trim().Equals("Insurance", StringComparison.OrdinalIgnoreCase));

        public bool IsHighValueClaim => (TotalInsPaid ?? 0) >= 500m;

        public bool IsNonElectronic => !string.IsNullOrWhiteSpace(MmRxOrigin) &&
            !MmRxOrigin.Trim().Equals("ELECTRONIC", StringComparison.OrdinalIgnoreCase);
    }

    public class ErxColumnMatchRuleDto
    {
        public int RuleId { get; set; }
        public string FieldGroupName { get; set; } = string.Empty;
        public string MmColumnName { get; set; } = string.Empty;
        public string ErxColumnName { get; set; } = string.Empty;
        public string MatchType { get; set; } = "Exact";
        public decimal? ToleranceValue { get; set; }
        public int? InsuranceCompanyId { get; set; }
        public string? InsuranceCompanyName { get; set; }
        public List<int> InsuranceCompanyIds { get; set; } = new();
        public List<string> InsuranceCompanyNames { get; set; } = new();
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; }
    }

    public class ErxAuditDiscrepancyDto
    {
        public long DiscrepancyId { get; set; }
        public long RecordId { get; set; }
        public Guid BatchId { get; set; }
        public string RxNumber { get; set; } = string.Empty;
        public int RuleId { get; set; }
        public string FieldGroupName { get; set; } = string.Empty;
        public string MmColumnName { get; set; } = string.Empty;
        public string ErxColumnName { get; set; } = string.Empty;
        public string? MmValue { get; set; }
        public string? ErxValue { get; set; }
        public string Status { get; set; } = "Open"; // Open, Reviewed, Ignored, Corrected
        public string? ReviewedBy { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public int RemainingBatchDiscrepancies { get; set; }
        public int RemainingRecordDiscrepancies { get; set; }
    }

    public class UpdateDiscrepancyStatusDto
    {
        public string Status { get; set; } = "Reviewed"; // Reviewed, Ignored, Corrected
        public string? ReviewedBy { get; set; }
        public string? ErxValue { get; set; }
        public string? MmValue { get; set; }
    }

    public class InsuranceCompanyDto
    {
        public int InsuranceCompanyId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public string? BINNumber { get; set; }
    }

    public class ErxAuditBatchUploadResponseDto
    {
        public Guid BatchId { get; set; }
        public Guid? PharmacyId { get; set; }
        public string? PharmacyName { get; set; }
        public string FileName { get; set; } = string.Empty;
        public int TotalRows { get; set; }
        public int TotalDiscrepancies { get; set; }
        public int DiscrepantRecordCount { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class ErxDiscrepancyColumnSummaryDto
    {
        public int RuleId { get; set; }
        public string FieldGroupName { get; set; } = string.Empty;
        public string MmColumnName { get; set; } = string.Empty;
        public string ErxColumnName { get; set; } = string.Empty;
        public int DiscrepancyCount { get; set; }
        public int RecordCount { get; set; }
    }

    public class BulkUpdateDiscrepancyStatusDto
    {
        public List<long>? DiscrepancyIds { get; set; }
        public List<long>? RecordIds { get; set; }
        public Guid? BatchId { get; set; }
        public string? DiscrepancyColumn { get; set; }
        public int? RuleId { get; set; }
        public string Status { get; set; } = "Ignored"; // Ignored, Reviewed, Open, Corrected
        public string? ReviewedBy { get; set; }
    }

    public class BulkUpdateDiscrepancyResponseDto
    {
        public int UpdatedCount { get; set; }
        public int RemainingBatchDiscrepancies { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<long> AffectedRecordIds { get; set; } = new();
    }

    public class UpdateErxAuditRecordDto
    {
        public string? RxNumber { get; set; }
        public string? MmFillNumber { get; set; }
        public string? ErxRxNumber { get; set; }

        public DateTime? MmOrderDate { get; set; }
        public DateTime? ErxOrderDate { get; set; }
        public DateTime? ErxEffectiveDate { get; set; }
        public DateTime? MmDateFilled { get; set; }
        public TimeSpan? MmTimeFilled { get; set; }
        public DateTime? PickupDate { get; set; }
        public DateTime? ErxReceivedDateTime { get; set; }

        public string? PaymentMethod { get; set; }
        public string? InsCode { get; set; }
        public string? BinNo { get; set; }
        public decimal? TotalInsPaid { get; set; }

        public string? MmPatientName { get; set; }
        public string? ErxPatientName { get; set; }

        public DateTime? MmPatientDob { get; set; }
        public DateTime? ErxPatientDob { get; set; }

        public string? MmPatientPhone { get; set; }
        public string? ErxPatientPhone { get; set; }

        public string? MmPatientAddress { get; set; }
        public string? ErxPatientAddress { get; set; }

        public string? MmPrescriberName { get; set; }
        public string? ErxPrescriberName { get; set; }
        public string? SupervisorName { get; set; }

        public string? MmPrescriberNpi { get; set; }
        public string? ErxPrescriberNpi { get; set; }

        public string? MmPrescriberDea { get; set; }
        public string? ErxPrescriberDea { get; set; }

        public string? MmDispensedDrugName { get; set; }
        public string? ErxDrugName { get; set; }

        public string? MmPotencyUnitCode { get; set; }
        public string? ErxPotencyUnitCode { get; set; }

        public string? MmDispensedNdc { get; set; }
        public string? ErxNdc { get; set; }

        public decimal? MmQtyOrd { get; set; }
        public decimal? ErxQtyOrd { get; set; }

        public decimal? MmQtyDispensed { get; set; }
        public decimal? ErxQtyDispensed { get; set; }

        public int? MmDispensedDaysSupply { get; set; }
        public int? ErxDaysSupply { get; set; }
        public int? ErxBasedDaysSupply { get; set; }

        public string? MmDispensedSig { get; set; }
        public string? ErxSig { get; set; }

        public string? TelephoneNotes { get; set; }
        public DateTime? TelephoneNotesDate { get; set; }
        public string? ErxNotes { get; set; }
        public DateTime? ErxNotesDate { get; set; }
        public string? ErxNotesCreatedBy { get; set; }

        public string? MmDawCode { get; set; }
        public string? ErxDawCode { get; set; }

        public int? MmRefillsPharmacistEntered { get; set; }
        public int? ErxRefill { get; set; }
        public string? MmRphNotes { get; set; }
        public bool? MmSignatureCaptured { get; set; }
        public string? FollowUpTag { get; set; }
        public string? MmDelivery { get; set; }
        public string? MmRxOrigin { get; set; }
        public decimal? MmCopay { get; set; }
        public string? DurReasonCd { get; set; }
        public string? DurProfCd { get; set; }
        public string? DurResultCode { get; set; }
        public string? Message { get; set; }
    }

    public class ErxAuditFilterOptionsDto
    {
        public List<string> PaymentMethods { get; set; } = new();
        public List<string> InsCodes { get; set; } = new();
        public List<string> BinNos { get; set; } = new();
        public List<string> RxOrigins { get; set; } = new();
        public int TotalRecords { get; set; }
        public int InsurancePriorityCount { get; set; }
        public int HighValueClaimCount { get; set; }
        public int NonElectronicCount { get; set; }

        public List<string> InsuranceCodes => InsCodes;
        public List<string> BinNumbers => BinNos;
        public int InsuranceRecordCount => InsurancePriorityCount;
        public int HighValueRecordCount => HighValueClaimCount;
        public int NonElectronicRecordCount => NonElectronicCount;
    }
}
