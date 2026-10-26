using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Rxintel.Generic.Common.Implementations.Paginations;
using Rxintel.Generic.Common.Implementations.Responses;
using Rxintel.Generic.Dtos.Dtos;

namespace Rxintel.Generic.Services.Interfaces
{
    public interface IErxAuditService
    {
        Task<PagedList<ErxAuditBatchDto>> GetBatchesAsync(PageParam pageParam, Guid? pharmacyId = null);
        Task<ErxAuditBatchDto?> GetBatchByIdAsync(Guid batchId);
        Task<PagedList<ErxAuditRecordDto>> GetBatchRecordsAsync(
            Guid batchId,
            PageParam pageParam,
            bool? discrepanciesOnly = null,
            string? discrepancyColumn = null,
            int? ruleId = null,
            string? paymentMethod = null,
            string? insCode = null,
            string? binNo = null,
            decimal? minTotalPaid = null,
            decimal? maxTotalPaid = null,
            bool? isHighValue = null,
            string? rxOrigin = null,
            bool? nonElectronicOnly = null,
            Guid? pharmacyId = null);

        Task<PagedList<ErxAuditRecordDto>> GetAllRecordsAsync(
            Guid? batchId,
            PageParam pageParam,
            bool? discrepanciesOnly = null,
            string? discrepancyColumn = null,
            int? ruleId = null,
            string? paymentMethod = null,
            string? insCode = null,
            string? binNo = null,
            decimal? minTotalPaid = null,
            decimal? maxTotalPaid = null,
            bool? isHighValue = null,
            string? rxOrigin = null,
            bool? nonElectronicOnly = null,
            Guid? pharmacyId = null);

        Task<ErxAuditFilterOptionsDto> GetFilterOptionsAsync(Guid? batchId = null, Guid? pharmacyId = null);
        Task<ErxAuditRecordDto?> GetRecordDiscrepanciesAsync(long recordId);
        Task<SuccessResult<ErxAuditDiscrepancyDto>> UpdateDiscrepancyStatusAsync(long discrepancyId, string status, string? reviewedBy = null, string? erxValue = null, string? mmValue = null);
        Task<SuccessResult<BulkUpdateDiscrepancyResponseDto>> BulkUpdateDiscrepancyStatusAsync(BulkUpdateDiscrepancyStatusDto dto);
        Task<List<ErxDiscrepancyColumnSummaryDto>> GetDiscrepancySummaryAsync(Guid? batchId = null, Guid? pharmacyId = null);
        Task<PagedList<ErxColumnMatchRuleDto>> GetRulesAsync(PageParam pageParam, int? insuranceCompanyId = null);
        Task<SuccessResult<ErxColumnMatchRuleDto>> CreateRuleAsync(ErxColumnMatchRuleDto dto);
        Task<SuccessResult<ErxColumnMatchRuleDto>> UpdateRuleAsync(int ruleId, ErxColumnMatchRuleDto dto);
        Task<SuccessResult<string>> DeleteRuleAsync(int ruleId);
        Task<SuccessResult<string>> DeleteBatchAsync(Guid batchId);
        Task<IEnumerable<InsuranceCompanyDto>> GetInsuranceCompaniesAsync();
        Task<byte[]> ExportRecordsAsync(Guid? batchId, bool? discrepanciesOnly = null, string format = "xlsx", Guid? pharmacyId = null);
        Task<int> ResolvePatientNameMatchingDemographicsAsync(Guid? batchId = null);
        Task<int> CleanupDuplicatePatientNameRulesAsync(Guid? batchId = null);
        Task<int> ResolveDaysSupplyCompliantDiscrepanciesAsync(Guid? batchId = null);
        Task<SuccessResult<ErxAuditRecordDto>> UpdateRecordAsync(long recordId, UpdateErxAuditRecordDto dto);
    }
}

