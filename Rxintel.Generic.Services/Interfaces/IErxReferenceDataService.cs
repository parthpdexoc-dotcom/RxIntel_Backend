using System.Threading.Tasks;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Entities.Entities;

namespace Rxintel.Generic.Services.Interfaces
{
    public interface IErxReferenceDataService
    {
        /// <summary>
        /// Loads or verifies that all static reference datasets (Controlled Drugs, State Validity, Supervising Physician, NPI Providers) are loaded in memory.
        /// </summary>
        Task EnsureLoadedAsync();

        /// <summary>
        /// Finds a controlled drug entry by NDC or Drug/Generic Name from the Scheduled Drugs reference file.
        /// </summary>
        ControlledDrugDto? FindControlledDrug(string? ndc, string? drugName = null);

        /// <summary>
        /// Checks if the drug is a controlled substance (Schedule II - V) based on NDC or Drug Name.
        /// </summary>
        bool IsControlledSubstance(string? ndc, string? drugName = null);

        /// <summary>
        /// Checks if the prescription record corresponds to a controlled substance based on dispensed/prescribed NDC or Drug Name.
        /// </summary>
        bool IsControlledSubstance(ErxAuditRecord? record);

        /// <summary>
        /// Returns DEA schedule number (2, 3, 4, 5) or null if non-controlled based on NDC or Drug Name.
        /// </summary>
        int? GetDeaSchedule(string? ndc, string? drugName = null);

        /// <summary>
        /// Returns DEA schedule number (2, 3, 4, 5) or null if non-controlled for the prescription record.
        /// </summary>
        int? GetDeaSchedule(ErxAuditRecord? record);

        /// <summary>
        /// Gets state-specific prescription validity limits (Schedule II, III-V, Non-controlled) by state name, code, or patient address.
        /// </summary>
        StateRxValidityDto? GetStateValidity(string? stateOrAddress);

        /// <summary>
        /// Gets state rules indicating whether PA and/or NP require a supervising physician name on the prescription.
        /// </summary>
        SupervisingPhysicianRuleDto? GetSupervisingRule(string? stateOrAddress);

        /// <summary>
        /// Looks up a provider in the active PA/NP directory by 10-digit NPI.
        /// </summary>
        NpiProviderDto? FindProviderByNpi(string? npi);

        /// <summary>
        /// Determines if a supervising physician name is required for the given prescriber NPI and prescription location.
        /// </summary>
        bool IsSupervisingPhysicianRequired(string? npi, string? stateOrAddress, out string providerType, out string resolvedState);

        /// <summary>
        /// Resolves a 2-letter US state code from one or more address strings or state names.
        /// </summary>
        string ResolveStateFromAddress(string? address1, string? address2 = null);
    }
}
