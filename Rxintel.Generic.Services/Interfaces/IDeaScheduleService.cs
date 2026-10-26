using Rxintel.Generic.Entities.Entities;

namespace Rxintel.Generic.Services.Interfaces
{
    public interface IDeaScheduleService
    {
        /// <summary>
        /// Identifies DEA Schedule (2, 3, 4, 5) or null if non-controlled based on Rx number
        /// </summary>
        int? GetDeaSchedule(string? rxNumber, string? erxRxNumber = null);

        /// <summary>
        /// Identifies DEA Schedule (2, 3, 4, 5) from Scheduled Drugs master reference or Rx number.
        /// </summary>
        int? GetDeaSchedule(ErxAuditRecord? record);

        /// <summary>
        /// Checks if prescription is a Schedule II (Class 2) Narcotic based on RX number.
        /// </summary>
        bool IsClass2Narcotic(string? rxNumber, string? erxRxNumber = null);

        /// <summary>
        /// Checks if prescription record is a Schedule II (Class 2) Narcotic.
        /// </summary>
        bool IsClass2Narcotic(ErxAuditRecord? record);

        /// <summary>
        /// Checks if prescription is a Schedule III, IV, or V Controlled Substance based on RX number.
        /// </summary>
        bool IsClass3To5Narcotic(string? rxNumber, string? erxRxNumber = null);

        /// <summary>
        /// Checks if prescription record is a Schedule III, IV, or V Controlled Substance.
        /// </summary>
        bool IsClass3To5Narcotic(ErxAuditRecord? record);

        /// <summary>
        /// Checks if prescription is a Controlled Substance (Schedule II - V).
        /// </summary>
        bool IsControlledSubstance(ErxAuditRecord? record);

        /// <summary>
        /// Returns friendly display label (e.g. "Class 2 Narcotic (Schedule II)").
        /// </summary>
        string GetScheduleLabel(int? schedule);
    }
}
