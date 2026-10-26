using System;

namespace Rxintel.Generic.Dtos.Dtos
{
    public class AuditReferenceDataOptions
    {
        public string? FolderPath { get; set; }
        public string? ControlledDrugsFile { get; set; }
        public string? StateValidityFile { get; set; }
        public string? SupervisingPhysicianFile { get; set; }
        public string? NpiDirectory { get; set; }
        public string? NpiBinaryCacheFile { get; set; }
    }

    public class ControlledDrugDto
    {
        public string ProductNdc { get; set; } = string.Empty;
        public string NormalizedNdc { get; set; } = string.Empty;
        public string BrandName { get; set; } = string.Empty;
        public string GenericName { get; set; } = string.Empty;
        public string? DosageForm { get; set; }
        public string? Route { get; set; }
        public string? Strength { get; set; }
        public string? Labeler { get; set; }
        public string DeaSchedule { get; set; } = string.Empty; // "II", "III", "IV", "V", "Not scheduled"
        public int? ScheduleNumber { get; set; } // 2, 3, 4, 5
        public bool IsControlled => ScheduleNumber.HasValue && ScheduleNumber.Value >= 2 && ScheduleNumber.Value <= 5;
    }

    public class StateRxValidityDto
    {
        public string StateName { get; set; } = string.Empty;
        public string StateCode { get; set; } = string.Empty; // e.g. "TX"
        public int? Schedule2ValidityDays { get; set; } // e.g. 30 for Texas
        public string? Schedule2RawText { get; set; }
        public int? Schedule3To5ValidityDays { get; set; } // e.g. 180
        public string? Schedule3To5RawText { get; set; }
        public int? NonControlledValidityDays { get; set; } // e.g. 365
        public string? NonControlledRawText { get; set; }
        public string? Notes { get; set; }
    }

    public class SupervisingPhysicianRuleDto
    {
        public string StateName { get; set; } = string.Empty;
        public string StateCode { get; set; } = string.Empty;
        public bool PaSupervisingRequired { get; set; }
        public string? PaRawText { get; set; }
        public bool NpSupervisingRequired { get; set; }
        public string? NpRawText { get; set; }
        public string? Notes { get; set; }
    }

    public class NpiProviderDto
    {
        public long Npi { get; set; }
        public string ProviderType { get; set; } = string.Empty; // "PA" or "NP"
        public string State { get; set; } = string.Empty;
    }
}
