using System;
using System.Text.RegularExpressions;
using Rxintel.Generic.Entities.Entities;
using Rxintel.Generic.Services.Interfaces;

namespace Rxintel.Generic.Services.Services
{
    public class DeaScheduleService : IDeaScheduleService
    {
        private readonly IErxReferenceDataService? _referenceDataService;

        public DeaScheduleService(IErxReferenceDataService? referenceDataService = null)
        {
            _referenceDataService = referenceDataService;
        }

        public int? GetDeaSchedule(string? rxNumber, string? erxRxNumber = null)
        {
            // Primary Rule:
            // Whatever RX Number is starting from 2 is known as Class 2.
            // Whatever RX Number is starting from 3, 4, 5 is known as Class 3 to 5.
            // This applies to both MICROMERCHANT RX and ERX RX.
            var mmClean = CleanRxNumber(rxNumber);
            var erxClean = CleanRxNumber(erxRxNumber);

            var mmHasVal = !string.IsNullOrEmpty(mmClean);
            var erxHasVal = !string.IsNullOrEmpty(erxClean);

            // 1. Check for Class 2 (RX Number starting with '2') on MicroMerchant or ERX
            if ((mmHasVal && mmClean!.StartsWith("2")) || (erxHasVal && erxClean!.StartsWith("2")))
            {
                return 2;
            }

            // 2. Check for Class 3 to 5 (RX Number starting with '3', '4', or '5') on MicroMerchant or ERX
            if (mmHasVal)
            {
                if (mmClean!.StartsWith("3")) return 3;
                if (mmClean!.StartsWith("4")) return 4;
                if (mmClean!.StartsWith("5")) return 5;
            }
            if (erxHasVal)
            {
                if (erxClean!.StartsWith("3")) return 3;
                if (erxClean!.StartsWith("4")) return 4;
                if (erxClean!.StartsWith("5")) return 5;
            }

            return null;
        }

        public int? GetDeaSchedule(ErxAuditRecord? record)
        {
            if (record == null) return null;

            // 1. Check authoritative Scheduled Drugs master list by NDC / Drug Name
            if (_referenceDataService != null)
            {
                var refSched = _referenceDataService.GetDeaSchedule(record);
                if (refSched.HasValue) return refSched.Value;
            }

            // 2. Fall back to Rx Number prefix heuristic
            return GetDeaSchedule(record.RxNumber, record.ErxRxNumber);
        }

        public bool IsClass2Narcotic(string? rxNumber, string? erxRxNumber = null)
        {
            var sched = GetDeaSchedule(rxNumber, erxRxNumber);
            return sched == 2;
        }

        public bool IsClass2Narcotic(ErxAuditRecord? record)
        {
            if (record == null) return false;

            if (_referenceDataService != null)
            {
                var refSched = _referenceDataService.GetDeaSchedule(record);
                if (refSched.HasValue) return refSched.Value == 2;
            }

            return IsClass2Narcotic(record.RxNumber, record.ErxRxNumber);
        }

        public bool IsClass3To5Narcotic(string? rxNumber, string? erxRxNumber = null)
        {
            var sched = GetDeaSchedule(rxNumber, erxRxNumber);
            return sched >= 3 && sched <= 5;
        }

        public bool IsClass3To5Narcotic(ErxAuditRecord? record)
        {
            if (record == null) return false;

            if (_referenceDataService != null)
            {
                var refSched = _referenceDataService.GetDeaSchedule(record);
                if (refSched.HasValue) return refSched.Value >= 3 && refSched.Value <= 5;
            }

            return IsClass3To5Narcotic(record.RxNumber, record.ErxRxNumber);
        }

        public bool IsControlledSubstance(ErxAuditRecord? record)
        {
            return IsClass2Narcotic(record) || IsClass3To5Narcotic(record);
        }

        public string GetScheduleLabel(int? schedule)
        {
            return schedule switch
            {
                2 => "Class 2 Narcotic (Schedule II)",
                3 => "Class 3 Controlled Substance (Schedule III)",
                4 => "Class 4 Controlled Substance (Schedule IV)",
                5 => "Class 5 Controlled Substance (Schedule V)",
                _ => "Non-Controlled"
            };
        }

        private static string? CleanRxNumber(string? rx)
        {
            if (string.IsNullOrWhiteSpace(rx)) return null;
            var trimmed = rx.Trim();
            // Strip any non-digit prefix like "RX#", "RX", "#", "NO", "MM-", etc.
            var cleaned = Regex.Replace(trimmed, @"^[^\d]+", "");
            if (string.IsNullOrEmpty(cleaned) || !char.IsDigit(cleaned[0]))
                return null;
            return cleaned;
        }
    }
}
