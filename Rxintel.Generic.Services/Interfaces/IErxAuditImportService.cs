using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Rxintel.Generic.Common.Implementations.Responses;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Entities.Entities;

namespace Rxintel.Generic.Services.Interfaces
{
    public interface IErxAuditImportService
    {
        /// <summary>
        /// Uploads and parses an ERx Audit Excel or CSV sheet, executes comparison engine, and logs alerts.
        /// </summary>
        Task<SuccessResult<ErxAuditBatchUploadResponseDto>> UploadAndProcessBatchAsync(IFormFile? file, string? uploadedBy = null, Guid? pharmacyId = null);

        /// <summary>
        /// Processes a stream of an ERx Audit file (used by uploads and auto-seed).
        /// </summary>
        Task<SuccessResult<ErxAuditBatchUploadResponseDto>> ProcessFileStreamAsync(System.IO.Stream stream, string fileName, string? uploadedBy = null, Guid? pharmacyId = null);

        /// <summary>
        /// Seeds/reloads sample uploaded data from the bundled sample or user's ERx file.
        /// </summary>
        Task<SuccessResult<ErxAuditBatchUploadResponseDto>> SeedSampleDataAsync();

        /// <summary>
        /// Processes pre-parsed audit rows for a batch.
        /// </summary>
        Task<int> ProcessBatchAsync(Guid batchId, IReadOnlyList<ErxAuditRecord> records);
    }
}
