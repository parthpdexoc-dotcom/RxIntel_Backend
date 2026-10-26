using System;

namespace Rxintel.Generic.Services.MetaAI
{
    /// <summary>
    /// Input payload for holistic prescription cross-field auditing by Muse AI.
    /// Captures all clinical, volumetric, prescriber credential, and date fields.
    /// </summary>
    public class MetaAiPrescriptionAuditRequest
    {
        public string? RxNumber { get; set; }
        public string? MmDrugName { get; set; }
        public string? ErxDrugName { get; set; }
        public string? MmDispensedNdc { get; set; }
        public string? ErxNdc { get; set; }
        public string? MmSig { get; set; }
        public string? ErxSig { get; set; }
        public int? MmDaysSupply { get; set; }
        public int? ErxDaysSupply { get; set; }
        public decimal? MmQtyDispensed { get; set; }
        public decimal? ErxQtyOrd { get; set; }
        public string? MmPrescriberName { get; set; }
        public string? ErxPrescriberName { get; set; }
        public string? SupervisorName { get; set; }
        public string? MmNpi { get; set; }
        public string? ErxNpi { get; set; }
        public string? MmDea { get; set; }
        public string? ErxDea { get; set; }
        public DateTime? MmOrderDate { get; set; }
        public DateTime? ErxOrderDate { get; set; }
        public DateTime? ErxEffectiveDate { get; set; }
        public DateTime? MmDateFilled { get; set; }
    }

    /// <summary>
    /// Comprehensive audit results evaluating interrelated prescription fields against clinical guidelines.
    /// </summary>
    public class MetaAiPrescriptionAuditResult
    {
        public bool IsOverallCompliant { get; set; }
        public double Confidence { get; set; }
        public string OverallReason { get; set; } = string.Empty;

        // Mathematical calculations derived directly from SIG
        public decimal? CalculatedDailyDose { get; set; }
        public int? CalculatedDaysSupply { get; set; }
        public decimal? CalculatedQuantityNeeded { get; set; }
        public string? DosingUnitType { get; set; }

        // Field-specific evaluations
        public bool IsSigCompliant { get; set; }
        public string SigReason { get; set; } = string.Empty;

        public bool IsDaysSupplyCompliant { get; set; }
        public string DaysSupplyReason { get; set; } = string.Empty;

        public bool IsQuantityCompliant { get; set; }
        public string QuantityReason { get; set; } = string.Empty;

        public bool IsDrugNdcCompliant { get; set; }
        public string DrugNdcReason { get; set; } = string.Empty;

        public bool IsPrescriberCompliant { get; set; }
        public string PrescriberReason { get; set; } = string.Empty;

        public bool IsDatesCompliant { get; set; }
        public string DatesReason { get; set; } = string.Empty;

        public long LatencyMs { get; set; }
        public bool FromCache { get; set; }
        public string Model { get; set; } = "muse-spark-1.3-contributor";
    }
}
