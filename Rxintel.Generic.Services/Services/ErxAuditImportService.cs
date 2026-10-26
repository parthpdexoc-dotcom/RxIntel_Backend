using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using Rxintel.Generic.Common.Helpers;
using Rxintel.Generic.Common.Implementations.Responses;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Entities.Entities;
using Rxintel.Generic.Repositories.Interfaces;
using Rxintel.Generic.Services.Interfaces;

namespace Rxintel.Generic.Services.Services
{
    /// <summary>
    /// Service responsible for importing, parsing, and orchestrating the audit comparison
    /// of prescription records between MicroMerchant (internal pharmacy dispensing system)
    /// and ERX (physician electronic prescribing system).
    /// 
    /// Purpose:
    /// - Ingests prescription data files in multiple formats (.xlsx, .xls, .xlsm, .csv).
    /// - Dynamically maps and normalizes vendor-specific column headers to internal data models.
    /// - Persists batches and individual prescription records into the database.
    /// - Executes the comparison engine against configured audit rules to detect clinical & operational discrepancies.
    /// - Generates audit alerts and records discrepancy entries for review by pharmacists and compliance teams.
    /// </summary>
    public class ErxAuditImportService : IErxAuditImportService
    {
        private readonly IErxAuditBatchRepository _batchRepository;
        private readonly IErxAuditRecordRepository _recordRepository;
        private readonly IErxColumnMatchRuleRepository _ruleRepository;
        private readonly IErxAuditDiscrepancyRepository _discrepancyRepository;
        private readonly IErxComparisonEngine _comparisonEngine;
        private readonly IErxAuditAlertService _alertService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ErxAuditImportService> _logger;
        private readonly ITokenParser? _tokenParser;
        private readonly IAutonomousAuditLearningService? _autonomousLearningService;

        private static readonly string[] UsaDateFormats = new[]
        {
            "M/d/yyyy", "MM/dd/yyyy",
            "M/d/yy", "MM/dd/yy",
            "M-d-yyyy", "MM-dd-yyyy",
            "M/d/yyyy H:mm", "MM/dd/yyyy H:mm",
            "M/d/yyyy HH:mm", "MM/dd/yyyy HH:mm",
            "M/d/yyyy h:mm tt", "MM/dd/yyyy h:mm tt",
            "M/d/yyyy hh:mm tt", "MM/dd/yyyy hh:mm tt",
            "M/d/yyyy H:mm:ss", "MM/dd/yyyy H:mm:ss",
            "M/d/yyyy HH:mm:ss", "MM/dd/yyyy HH:mm:ss",
            "M/d/yyyy h:mm:ss tt", "MM/dd/yyyy h:mm:ss tt",
            "M/d/yyyy hh:mm:ss tt", "MM/dd/yyyy hh:mm:ss tt",
            "yyyy-MM-dd", "yyyy-MM-dd HH:mm:ss", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-ddTHH:mm:ssZ"
        };

        /// <summary>
        /// Constructor for ErxAuditImportService.
        /// Purpose: Injects repositories for database persistence, comparison engine for evaluating rules,
        /// alert service for discrepancy notifications, unit of work, logging infrastructure, and token parser for user identity.
        /// </summary>
        public ErxAuditImportService(
            IErxAuditBatchRepository batchRepository,
            IErxAuditRecordRepository recordRepository,
            IErxColumnMatchRuleRepository ruleRepository,
            IErxAuditDiscrepancyRepository discrepancyRepository,
            IErxComparisonEngine comparisonEngine,
            IErxAuditAlertService alertService,
            IUnitOfWork unitOfWork,
            ILogger<ErxAuditImportService> logger,
            ITokenParser? tokenParser = null,
            IAutonomousAuditLearningService? autonomousLearningService = null)
        {
            _batchRepository = batchRepository;
            _recordRepository = recordRepository;
            _ruleRepository = ruleRepository;
            _discrepancyRepository = discrepancyRepository;
            _comparisonEngine = comparisonEngine;
            _alertService = alertService;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _tokenParser = tokenParser;
            _autonomousLearningService = autonomousLearningService;
        }

        /// <summary>
        /// Uploads and processes a prescription audit file received via HTTP multipart request.
        /// 
        /// Purpose:
        /// - Serves as the primary entry point for the frontend file upload UI.
        /// - Resolves uploader identity from the authenticated token context when not explicitly provided.
        /// - Validates file presence, size, and supported extensions (.xlsx, .xls, .xlsm, .csv).
        /// - Opens a readable stream and delegates execution to <see cref="ProcessFileStreamAsync"/>.
        /// </summary>
        /// <param name="file">The uploaded file from the client form submission.</param>
        /// <param name="uploadedBy">Optional username or identifier of the user uploading the file (defaults to current token user).</param>
        /// <returns>A response DTO containing the batch ID, row counts, discrepancy counts, and status.</returns>
        public async Task<SuccessResult<ErxAuditBatchUploadResponseDto>> UploadAndProcessBatchAsync(IFormFile? file, string? uploadedBy = null, Guid? pharmacyId = null)
        {
            if (string.IsNullOrWhiteSpace(uploadedBy) && _tokenParser != null)
            {
                var tokenUser = _tokenParser.GetName();
                if (string.IsNullOrWhiteSpace(tokenUser))
                    tokenUser = _tokenParser.GetEmail();
                if (!string.IsNullOrWhiteSpace(tokenUser))
                    uploadedBy = tokenUser;
            }

            if (!pharmacyId.HasValue && _tokenParser != null)
            {
                pharmacyId = _tokenParser.GetPharmacyId();
            }

            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("[AuditImport:Upload] Empty or missing file submitted by '{User}'.", uploadedBy);
                return SuccessResult<ErxAuditBatchUploadResponseDto>.BadRequest("Please upload a valid Excel or CSV file.");
            }

            var fileName = ImportFileHelper.GetUploadedFileName(file);
            _logger.LogInformation("[AuditImport:Upload] Received file '{FileName}' ({Length} bytes) from '{User}' for Pharmacy '{PharmacyId}'.", fileName, file.Length, uploadedBy, pharmacyId);

            if (!ImportFileHelper.IsSupportedImportFile(fileName))
            {
                _logger.LogWarning("[AuditImport:Upload] Unsupported file format '{FileName}' from '{User}'.", fileName, uploadedBy);
                return SuccessResult<ErxAuditBatchUploadResponseDto>.BadRequest("Unsupported file format. Please upload an Excel (.xlsx, .xls, .xlsm) or CSV file.");
            }

            await using var stream = file.OpenReadStream();
            return await ProcessFileStreamAsync(stream, fileName, uploadedBy, pharmacyId);
        }

        /// <summary>
        /// Core orchestration method that parses a raw file stream, saves the batch, and triggers the audit.
        /// 
        /// Purpose:
        /// - Generates a new unique BatchId for tracking the upload lifecycle.
        /// - Parses spreadsheet rows into strongly-typed <see cref="ErxAuditRecord"/> entities.
        /// - Creates an initial 'Processing' batch record in the database.
        /// - Invokes <see cref="ProcessBatchAsync"/> to run comparison rules and find discrepancies.
        /// - Updates the batch status to 'Completed' (with discrepancy statistics) or 'Failed' upon error.
        /// </summary>
        /// <param name="stream">Readable stream containing file content.</param>
        /// <param name="fileName">Original file name (used for format detection and batch metadata).</param>
        /// <param name="uploadedBy">Name of the user initiating the processing.</param>
        /// <param name="pharmacyId">Optional target Pharmacy ID.</param>
        /// <returns>Summary DTO containing batch metrics, discrepancy counts, and user message.</returns>
        public async Task<SuccessResult<ErxAuditBatchUploadResponseDto>> ProcessFileStreamAsync(Stream stream, string fileName, string? uploadedBy, Guid? pharmacyId = null)
        {
            var batchId = Guid.NewGuid();
            if (!pharmacyId.HasValue && _tokenParser != null)
            {
                pharmacyId = _tokenParser.GetPharmacyId();
            }

            _logger.LogInformation("[AuditImport:ProcessStream] Beginning stream processing for '{FileName}' (Assigned BatchId: {BatchId}, InitiatedBy: '{User}', PharmacyId: {PharmacyId}).",
                fileName, batchId, uploadedBy, pharmacyId);

            List<ErxAuditRecord> records;
            try
            {
                records = ParseRecordsFromStream(stream, fileName, batchId);
                _logger.LogInformation("[AuditImport:ProcessStream] Parsed {Count} prescription rows from '{FileName}' for BatchId {BatchId}.",
                    records.Count, fileName, batchId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse ERx Audit file {FileName}", fileName);
                return SuccessResult<ErxAuditBatchUploadResponseDto>.BadRequest($"Failed to parse file: {ex.Message}");
            }

            var batch = new ErxAuditBatch
            {
                BatchId = batchId,
                PharmacyId = pharmacyId,
                FileName = fileName,
                UploadedBy = string.IsNullOrWhiteSpace(uploadedBy) ? "Pharmacist/Admin" : uploadedBy.Trim(),
                UploadedAt = DateTime.UtcNow,
                TotalRows = records.Count,
                TotalDiscrepancies = 0,
                Status = "Processing"
            };

            await _batchRepository.UpsertAsync(batch, "BatchId");

            if (records.Count == 0)
            {
                _logger.LogWarning("[AuditImport:ProcessStream] No valid rows found in '{FileName}' for BatchId {BatchId}.", fileName, batchId);
                batch.Status = "Completed";
                batch.TotalRows = 0;
                batch.TotalDiscrepancies = 0;
                await _batchRepository.UpsertAsync(batch, "BatchId");

                var emptyMsg = "File was processed successfully, but no prescription rows were found.";
                return SuccessResult<ErxAuditBatchUploadResponseDto>.Ok(new ErxAuditBatchUploadResponseDto
                {
                    BatchId = batch.BatchId,
                    PharmacyId = batch.PharmacyId,
                    FileName = fileName,
                    TotalRows = 0,
                    TotalDiscrepancies = 0,
                    DiscrepantRecordCount = 0,
                    Message = emptyMsg
                }, emptyMsg);
            }

            try
            {
                var totalDiscrepancies = await ProcessBatchAsync(batch.BatchId, records);

                batch.TotalRows = records.Count;
                batch.TotalDiscrepancies = totalDiscrepancies;
                batch.Status = "Completed";
                await _batchRepository.UpsertAsync(batch, "BatchId");

                var discrepantRows = records.Count(r => r.Discrepancies.Count > 0);

                var summaryMessage = totalDiscrepancies > 0
                    ? $"Batch processed successfully: {totalDiscrepancies} discrepancies found across {discrepantRows} prescription(s)."
                    : $"Batch processed successfully: all {records.Count} prescriptions matched with 0 discrepancies.";

                _logger.LogInformation("[AuditImport:ProcessStream] BatchId {BatchId} ('{FileName}') completed: {Discrepancies} discrepancies found across {DiscrepantRows} prescription(s) out of {Total} total rows.",
                    batchId, fileName, totalDiscrepancies, discrepantRows, records.Count);

                return SuccessResult<ErxAuditBatchUploadResponseDto>.Ok(new ErxAuditBatchUploadResponseDto
                {
                    BatchId = batch.BatchId,
                    PharmacyId = batch.PharmacyId,
                    FileName = fileName,
                    TotalRows = records.Count,
                    TotalDiscrepancies = totalDiscrepancies,
                    DiscrepantRecordCount = discrepantRows,
                    Message = summaryMessage
                }, summaryMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process batch {BatchId} for file {FileName}", batch.BatchId, fileName);
                batch.Status = "Failed";
                try
                {
                    await _batchRepository.UpsertAsync(batch, "BatchId");
                }
                catch { }
                return SuccessResult<ErxAuditBatchUploadResponseDto>.BadRequest($"Failed to process batch: {ex.Message}");
            }
        }

        /// <summary>
        /// Automatically discovers and imports a bundled sample audit file (e.g. 'ERx_Audit_Sample.xlsx').
        /// 
        /// Purpose:
        /// - Provides one-click sample data loading for local development, testing, and UI demonstrations.
        /// - Scans common candidate directories for the sample Excel workbook and executes the full import flow.
        /// </summary>
        /// <returns>A response DTO detailing the seeded batch and comparison results.</returns>
        public async Task<SuccessResult<ErxAuditBatchUploadResponseDto>> SeedSampleDataAsync()
        {
            _logger.LogInformation("[AuditImport:SeedSampleData] Searching for sample ERx workbook to seed...");

            var candidates = new[]
            {
                @"C:\Users\Parth_2\Downloads\Audit_ERx_Workbook.xlsx",
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Audit_ERx_Workbook.xlsx"),
                Path.Combine(Directory.GetCurrentDirectory(), "Data", "Audit_ERx_Workbook.xlsx"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "ERx_Audit_Sample.xlsx"),
                Path.Combine(Directory.GetCurrentDirectory(), "Data", "ERx_Audit_Sample.xlsx"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Data", "ERx_Audit_Sample.xlsx"),
                Path.Combine("F:\\Projects\\RX_INTEL_NEW\\RxIntel_Backend\\Rxintel.AI\\Data", "ERx_Audit_Sample.xlsx"),
                @"C:\Users\Parth_2\Downloads\ERx Audit.xlsx"
            };

            string? foundPath = candidates.FirstOrDefault(File.Exists);
            if (foundPath == null)
            {
                _logger.LogWarning("[AuditImport:SeedSampleData] No sample ERx file found across candidate paths.");
                return SuccessResult<ErxAuditBatchUploadResponseDto>.BadRequest("No sample ERx Audit file found to seed data.");
            }

            _logger.LogInformation("[AuditImport:SeedSampleData] Sample file located at '{Path}'. Starting seed process...", foundPath);
            await using var stream = new FileStream(foundPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            return await ProcessFileStreamAsync(stream, Path.GetFileName(foundPath), "Pharmacist/Admin");
        }

        /// <summary>
        /// Executes the discrepancy audit comparison workflow on a collection of prescription records.
        /// 
        /// Purpose:
        /// - Assigns the BatchId to all parsed records and bulk-inserts them into the database so DB-generated IDs are created.
        /// - Loads all active column match rules configured in the system.
        /// - Evaluates each record against the comparison engine to identify mismatches (Exact, Fuzzy, Numeric, Dates, DEA schedules).
        /// - Dispatches automated notifications/alerts for any discovered discrepancies via <see cref="_alertService"/>.
        /// - Bulk-persists all detected discrepancies into the database for pharmacist review and tracking.
        /// </summary>
        /// <param name="batchId">The unique identifier of the batch.</param>
        /// <param name="records">List of parsed prescription records to audit.</param>
        /// <returns>The total number of discrepancies detected across the batch.</returns>
        public async Task<int> ProcessBatchAsync(Guid batchId, IReadOnlyList<ErxAuditRecord> records)
        {
            if (records == null || records.Count == 0) return 0;

            _logger.LogInformation("[AuditImport:ProcessBatch] Bulk inserting {Count} audit records for BatchId {BatchId}...", records.Count, batchId);

            // Set BatchId and bulk save records so RecordIds are generated by DB
            foreach (var record in records)
            {
                record.BatchId = batchId;
            }
            await _recordRepository.AddRangeAsync(records);

            // Load active match rules, filtering out any duplicate test rules
            var activeRules = (await _ruleRepository.GetAllAsync(r => r.IsActive))
                .Where(r => !r.FieldGroupName.Contains("Testing", StringComparison.OrdinalIgnoreCase) &&
                            !r.FieldGroupName.Contains("Comparision", StringComparison.OrdinalIgnoreCase))
                .ToList();

            _logger.LogInformation("[AuditImport:ProcessBatch] Records saved. Evaluating {RuleCount} active rules across {RecordCount} records...",
                activeRules.Count, records.Count);

            var allDiscrepancies = new List<ErxAuditDiscrepancy>();
            var recordsWithDiscrepancies = new List<(ErxAuditRecord Record, IReadOnlyList<ErxAuditDiscrepancy> Discrepancies)>();

            // Run comparison engine per record
            foreach (var record in records)
            {
                try
                {
                    var discrepancies = _comparisonEngine.CompareRecord(record, activeRules);
                    foreach (var disc in discrepancies)
                    {
                        disc.RecordId = record.RecordId;
                        allDiscrepancies.Add(disc);
                    }

                    if (discrepancies.Count > 0)
                    {
                        recordsWithDiscrepancies.Add((record, discrepancies));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not compare record Rx {RxNumber}", record.RxNumber);
                }
            }

            _logger.LogInformation("[AuditImport:ProcessBatch] Comparison completed for BatchId {BatchId}: {DiscrepancyCount} total discrepancies found.",
                batchId, allDiscrepancies.Count);

            if (allDiscrepancies.Count > 0)
            {
                try
                {
                    await _discrepancyRepository.AddRangeAsync(allDiscrepancies);
                    _logger.LogInformation("[AuditImport:ProcessBatch] Successfully persisted {Count} discrepancies to database for BatchId {BatchId}.",
                        allDiscrepancies.Count, batchId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to persist discrepancies for batch {BatchId}", batchId);
                }
            }

            // Dispatch batch alerts in bulk (1 database write instead of N sequential roundtrips)
            if (recordsWithDiscrepancies.Count > 0)
            {
                try
                {
                    await _alertService.LogAndSendBatchAlertsAsync(recordsWithDiscrepancies);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not dispatch batch alerts for BatchId {BatchId}", batchId);
                }
            }

            // Autonomous AI Learning: Run immediate autonomous sweep so Muse AI learns patterns and auto-resolves false discrepancies immediately
            var finalDiscrepancyCount = allDiscrepancies.Count;
            if (allDiscrepancies.Count > 0 && _autonomousLearningService != null)
            {
                try
                {
                    _logger.LogInformation("[AuditImport:ProcessBatch] Launching autonomous Muse AI learning sweep for BatchId {BatchId}...", batchId);
                    var autoReport = await _autonomousLearningService.RunAutonomousLearningAsync(batchId);
                    _logger.LogInformation("[AuditImport:ProcessBatch] Autonomous learning complete for BatchId {BatchId}: {Resolved} discrepancies auto-resolved, {Memories} new memories learned.",
                        batchId, autoReport.AutonomouslyResolvedCount, autoReport.NewMemoriesLearned);

                    finalDiscrepancyCount = Math.Max(0, allDiscrepancies.Count - autoReport.AutonomouslyResolvedCount);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[AuditImport:ProcessBatch] Autonomous learning sweep failed for BatchId {BatchId}", batchId);
                }
            }

            return finalDiscrepancyCount;
        }

        /// <summary>
        /// Dispatcher method that routes a file stream to the appropriate parsing implementation based on file extension.
        /// 
        /// Purpose:
        /// - Detects modern OpenXML Excel (.xlsx, .xlsm), legacy Excel (.xls), or CSV format.
        /// - Isolates file format specifics behind a common list-of-records interface.
        /// </summary>
        /// <param name="stream">The file stream to parse.</param>
        /// <param name="fileName">Name of the file to determine extension.</param>
        /// <param name="batchId">Batch ID to assign to the extracted records.</param>
        /// <returns>List of parsed <see cref="ErxAuditRecord"/> entities.</returns>
        private List<ErxAuditRecord> ParseRecordsFromStream(Stream stream, string fileName, Guid batchId)
        {
            if (ImportFileHelper.IsLegacyExcelFile(fileName))
            {
                return ParseLegacyExcel(stream, batchId);
            }

            if (ImportFileHelper.IsExcelFile(fileName))
            {
                return ParseOpenXmlExcel(stream, batchId);
            }

            return ParseCsv(stream, batchId);
        }

        /// <summary>
        /// Parses modern OpenXML Excel workbooks (.xlsx, .xlsm) using ClosedXML.
        /// 
        /// Purpose:
        /// - Reads header rows to establish dynamic column mappings.
        /// - Iterates over rows, extracts cell values safely across various Excel data types,
        ///   and converts each valid row into an <see cref="ErxAuditRecord"/>.
        /// - Automatically handles fallback RxNumber generation if only ERX RxNumber is present.
        /// </summary>
        /// <param name="stream">The stream containing OpenXML spreadsheet data.</param>
        /// <param name="batchId">The audit batch ID to associate with each record.</param>
        /// <returns>List of mapped <see cref="ErxAuditRecord"/> entities.</returns>
        private List<ErxAuditRecord> ParseOpenXmlExcel(Stream stream, Guid batchId)
        {
            var records = new List<ErxAuditRecord>();
            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheets.FirstOrDefault();
            if (worksheet == null) return records;

            var usedRange = worksheet.RangeUsed();
            if (usedRange == null) return records;

            var firstRow = usedRange.FirstRow().RowNumber();
            var lastRow = usedRange.LastRow().RowNumber();
            var firstCol = usedRange.FirstColumn().ColumnNumber();
            var lastCol = usedRange.LastColumn().ColumnNumber();

            var headers = new List<string>();
            for (var col = firstCol; col <= lastCol; col++)
            {
                headers.Add(GetCellString(worksheet.Cell(firstRow, col)));
            }

            var columnMap = BuildColumnMapping(headers);

            for (var rowNum = firstRow + 1; rowNum <= lastRow; rowNum++)
            {
                var row = worksheet.Row(rowNum);
                var rowData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var hasAnyData = false;

                for (var col = firstCol; col <= lastCol; col++)
                {
                    var colIndex = col - firstCol;
                    if (colIndex < headers.Count)
                    {
                        var cellVal = GetCellString(worksheet.Cell(rowNum, col));
                        if (!string.IsNullOrEmpty(cellVal)) hasAnyData = true;
                        rowData[headers[colIndex]] = cellVal;
                    }
                }

                if (!hasAnyData) continue;

                var record = MapRowToRecord(rowData, columnMap, batchId);
                if (string.IsNullOrWhiteSpace(record.RxNumber) && !string.IsNullOrWhiteSpace(record.ErxRxNumber))
                {
                    record.RxNumber = record.ErxRxNumber;
                }

                if (!string.IsNullOrWhiteSpace(record.RxNumber) ||
                    !string.IsNullOrWhiteSpace(record.MmDispensedDrugName) ||
                    !string.IsNullOrWhiteSpace(record.ErxDrugName))
                {
                    if (string.IsNullOrWhiteSpace(record.RxNumber))
                    {
                        record.RxNumber = "RX-" + (records.Count + 1).ToString("D6");
                    }
                    records.Add(record);
                }
            }

            return records;
        }

        /// <summary>
        /// Parses legacy Excel workbooks (.xls / BIFF8) using NPOI HSSFWorkbook.
        /// 
        /// Purpose:
        /// - Provides backwards compatibility for older pharmacy PMS exports that generate binary .xls files.
        /// - Reads sheet data, headers, and cell types (dates, formulas, numbers, strings) into audit records.
        /// </summary>
        /// <param name="stream">The stream containing legacy binary Excel data.</param>
        /// <param name="batchId">The audit batch ID to associate with each record.</param>
        /// <returns>List of mapped <see cref="ErxAuditRecord"/> entities.</returns>
        private List<ErxAuditRecord> ParseLegacyExcel(Stream stream, Guid batchId)
        {
            var records = new List<ErxAuditRecord>();
            var workbook = new HSSFWorkbook(stream);
            var sheet = workbook.GetSheetAt(0);
            if (sheet == null || sheet.PhysicalNumberOfRows == 0) return records;

            var headerRow = sheet.GetRow(sheet.FirstRowNum);
            if (headerRow == null) return records;

            var headers = new List<string>();
            for (var col = 0; col < headerRow.LastCellNum; col++)
            {
                var cell = headerRow.GetCell(col);
                headers.Add(GetLegacyCellString(cell));
            }

            var columnMap = BuildColumnMapping(headers);

            for (var rowNum = sheet.FirstRowNum + 1; rowNum <= sheet.LastRowNum; rowNum++)
            {
                var row = sheet.GetRow(rowNum);
                if (row == null) continue;

                var rowData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var hasAnyData = false;

                for (var col = 0; col < headers.Count; col++)
                {
                    var cell = row.GetCell(col);
                    var cellVal = GetLegacyCellString(cell);
                    if (!string.IsNullOrEmpty(cellVal)) hasAnyData = true;
                    rowData[headers[col]] = cellVal;
                }

                if (!hasAnyData) continue;

                var record = MapRowToRecord(rowData, columnMap, batchId);
                if (string.IsNullOrWhiteSpace(record.RxNumber) && !string.IsNullOrWhiteSpace(record.ErxRxNumber))
                {
                    record.RxNumber = record.ErxRxNumber;
                }

                if (!string.IsNullOrWhiteSpace(record.RxNumber) ||
                    !string.IsNullOrWhiteSpace(record.MmDispensedDrugName) ||
                    !string.IsNullOrWhiteSpace(record.ErxDrugName))
                {
                    if (string.IsNullOrWhiteSpace(record.RxNumber))
                    {
                        record.RxNumber = "RX-" + (records.Count + 1).ToString("D6");
                    }
                    records.Add(record);
                }
            }

            return records;
        }

        /// <summary>
        /// Parses Comma-Separated Values (CSV) files into prescription audit records.
        /// 
        /// Purpose:
        /// - Ingests lightweight text-based export files from pharmacy systems.
        /// - Uses RFC-compliant CSV line parsing to handle escaped quotes and embedded commas.
        /// - Maps header tokens and translates rows into <see cref="ErxAuditRecord"/> entities.
        /// </summary>
        /// <param name="stream">The stream containing raw CSV text.</param>
        /// <param name="batchId">The audit batch ID to associate with each record.</param>
        /// <returns>List of mapped <see cref="ErxAuditRecord"/> entities.</returns>
        private List<ErxAuditRecord> ParseCsv(Stream stream, Guid batchId)
        {
            var records = new List<ErxAuditRecord>();
            using var reader = new StreamReader(stream);
            var headerLine = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(headerLine)) return records;

            var headers = FieldMappingCsvHelper.ParseCsvHeader(headerLine);
            var columnMap = BuildColumnMapping(headers);

            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var cells = FieldMappingCsvHelper.ParseCsvHeader(line);
                var rowData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                for (var i = 0; i < headers.Count && i < cells.Count; i++)
                {
                    rowData[headers[i]] = cells[i]?.Trim() ?? string.Empty;
                }

                var record = MapRowToRecord(rowData, columnMap, batchId);
                if (string.IsNullOrWhiteSpace(record.RxNumber) && !string.IsNullOrWhiteSpace(record.ErxRxNumber))
                {
                    record.RxNumber = record.ErxRxNumber;
                }

                if (!string.IsNullOrWhiteSpace(record.RxNumber) ||
                    !string.IsNullOrWhiteSpace(record.MmDispensedDrugName) ||
                    !string.IsNullOrWhiteSpace(record.ErxDrugName))
                {
                    if (string.IsNullOrWhiteSpace(record.RxNumber))
                    {
                        record.RxNumber = "RX-" + (records.Count + 1).ToString("D6");
                    }
                    records.Add(record);
                }
            }

            return records;
        }

        /// <summary>
        /// Extracts and formats cell values from a ClosedXML <see cref="IXLCell"/> into a normalized string.
        /// 
        /// Purpose:
        /// - Handles Excel date formats (formatting to 'yyyy-MM-dd HH:mm:ss' in InvariantCulture).
        /// - Formats integer and decimal numbers without unwanted trailing zeros or exponent notation.
        /// - Handles TimeSpans, Booleans, and formatted strings to ensure reliable downstream data parsing.
        /// </summary>
        /// <param name="cell">The ClosedXML cell to inspect.</param>
        /// <returns>Cleaned string representation of the cell's underlying value.</returns>
        private static string GetCellString(IXLCell cell)
        {
            if (cell.IsEmpty())
                return string.Empty;

            if (cell.DataType == XLDataType.DateTime)
            {
                var fmtStr = cell.GetFormattedString()?.Trim();
                if (!string.IsNullOrEmpty(fmtStr) &&
                    DateTime.TryParseExact(fmtStr, UsaDateFormats, CultureInfo.GetCultureInfo("en-US"), DateTimeStyles.None, out var exactUsDt))
                {
                    return exactUsDt.ToString("MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                }

                var dt = cell.GetDateTime();
                return dt.ToString("MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
            }

            if (cell.DataType == XLDataType.TimeSpan)
                return cell.GetTimeSpan().ToString();

            if (cell.DataType == XLDataType.Number)
            {
                var numFmt = cell.Style.NumberFormat?.Format;
                var isDateFmt = (!string.IsNullOrWhiteSpace(numFmt) && (numFmt.Contains("yy", StringComparison.OrdinalIgnoreCase) || numFmt.Contains("dd", StringComparison.OrdinalIgnoreCase) || numFmt.Contains("mm", StringComparison.OrdinalIgnoreCase)))
                                || (cell.Style.DateFormat != null && !string.IsNullOrWhiteSpace(cell.Style.DateFormat.Format));
                if (isDateFmt)
                {
                    try
                    {
                        var fmtStr = cell.GetFormattedString()?.Trim();
                        if (!string.IsNullOrEmpty(fmtStr) &&
                            DateTime.TryParseExact(fmtStr, UsaDateFormats, CultureInfo.GetCultureInfo("en-US"), DateTimeStyles.None, out var exactUsDt))
                        {
                            return exactUsDt.ToString("MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                        }

                        var dt = cell.GetDateTime();
                        return dt.ToString("MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                    }
                    catch
                    {
                        // ignore
                    }
                }

                var value = cell.GetDouble();
                return value == Math.Truncate(value)
                    ? ((long)value).ToString(CultureInfo.InvariantCulture)
                    : value.ToString(CultureInfo.InvariantCulture);
            }

            if (cell.DataType == XLDataType.Boolean)
            {
                return cell.GetBoolean() ? "true" : "false";
            }

            var str = cell.GetString()?.Trim();
            if (!string.IsNullOrEmpty(str))
                return str;

            return cell.GetFormattedString().Trim();
        }

        /// <summary>
        /// Extracts and formats cell values from an NPOI <see cref="ICell"/> (legacy Excel format).
        /// 
        /// Purpose:
        /// - Handles NPOI date cells, numeric values, formula calculation results, and text cells.
        /// - Standardizes output so legacy .xls files yield identical string formats as modern .xlsx files.
        /// </summary>
        /// <param name="cell">The NPOI cell to inspect.</param>
        /// <returns>Normalized string value of the legacy cell.</returns>
        private static string GetLegacyCellString(ICell? cell)
        {
            if (cell == null) return string.Empty;
            return cell.CellType switch
            {
                CellType.Numeric when DateUtil.IsCellDateFormatted(cell) =>
                    cell.DateCellValue?.ToString("MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture) ?? string.Empty,
                CellType.Numeric => cell.NumericCellValue.ToString(CultureInfo.InvariantCulture),
                CellType.Boolean => cell.BooleanCellValue.ToString(),
                CellType.String => cell.StringCellValue?.Trim() ?? string.Empty,
                CellType.Formula => cell.CachedFormulaResultType switch
                {
                    CellType.Numeric when DateUtil.IsCellDateFormatted(cell) =>
                        cell.DateCellValue?.ToString("MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture) ?? string.Empty,
                    CellType.Numeric => cell.NumericCellValue.ToString(CultureInfo.InvariantCulture),
                    CellType.Boolean => cell.BooleanCellValue.ToString(),
                    CellType.String => cell.StringCellValue?.Trim() ?? string.Empty,
                    _ => string.Empty
                },
                _ => cell.ToString()?.Trim() ?? string.Empty
            };
        }

        /// <summary>
        /// Builds a lookup dictionary that maps raw spreadsheet headers to <see cref="ErxAuditRecord"/> property names.
        /// 
        /// Purpose:
        /// - Accommodates diverse spreadsheet naming conventions across different pharmacy PMS and e-prescribing systems.
        /// - Checks direct property name matches, normalized matches, and synonyms via <see cref="ResolveHeaderAlias"/>.
        /// </summary>
        /// <param name="headers">List of header column names extracted from row 1 of the file.</param>
        /// <returns>Dictionary with raw header as key and destination entity property name as value.</returns>
        private static Dictionary<string, string> BuildColumnMapping(List<string> headers)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var propertyNames = typeof(ErxAuditRecord).GetProperties()
                .Where(p => !p.Name.Equals(nameof(ErxAuditRecord.RecordId), StringComparison.OrdinalIgnoreCase))
                .Select(p => p.Name)
                .ToList();

            foreach (var header in headers)
            {
                if (string.IsNullOrWhiteSpace(header)) continue;

                var norm = NormalizeHeader(header);

                // Primary identity keys must never be mapped from import files
                if (norm.Equals("RECORDID", StringComparison.OrdinalIgnoreCase) ||
                    norm.Equals("ID", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Direct or normalized match with ErxAuditRecord properties
                var matchedProp = propertyNames.FirstOrDefault(p =>
                    string.Equals(p, norm, StringComparison.OrdinalIgnoreCase));

                if (matchedProp != null)
                {
                    map[header] = matchedProp;
                    continue;
                }

                // Header alias matching
                var alias = ResolveHeaderAlias(norm);
                if (alias != null)
                {
                    map[header] = alias;
                }
            }

            return map;
        }

        /// <summary>
        /// Normalizes a header string by standardizing common vendor prefixes and stripping symbols.
        /// 
        /// Purpose:
        /// - Standardizes "MICROMERCHANT" and "MICRO MERCHANT" to "MM".
        /// - Removes spaces, underscores, dashes, hashes, slashes, and parentheses for fuzzy header matching.
        /// </summary>
        /// <param name="header">Raw header text from the file.</param>
        /// <returns>Sanitized uppercase alphanumeric header string.</returns>
        private static string NormalizeHeader(string header)
        {
            if (string.IsNullOrWhiteSpace(header)) return string.Empty;
            var h = header.Trim();
            h = Regex.Replace(h, @"\bMICROMERCHANT\b", "MM", RegexOptions.IgnoreCase);
            h = Regex.Replace(h, @"\bMICRO\s*MERCHANT\b", "MM", RegexOptions.IgnoreCase);
            h = Regex.Replace(h, @"[\s_\-#/()]+", "").Trim();
            if (h.StartsWith("MICROMERCHANT", StringComparison.OrdinalIgnoreCase))
            {
                h = "MM" + h.Substring("MICROMERCHANT".Length);
            }
            return h;
        }

        /// <summary>
        /// Resolves common synonyms, abbreviations, and vendor-specific column names to canonical entity properties.
        /// 
        /// Purpose:
        /// - Maps variations for:
        ///   * Prescription identifiers (RxNumber, FillNumber, ErxRxNumber)
        ///   * Dates (OrderDate, EffectiveDate, DateFilled, TimeFilled, PickupDate, ReceivedDate)
        ///   * Patient demographics (Name, DOB, Phone, Address)
        ///   * Prescriber credentials (Name, Supervisor, NPI, DEA)
        ///   * Medication attributes (DrugName, PotencyUnit, NDC, QtyOrdered, QtyDispensed, DaysSupply, Sig)
        ///   * Clinical & operational fields (DAW, Refills, Copay, Delivery, Origin, Signature, Notes, DUR codes)
        /// </summary>
        /// <param name="norm">Normalized header string.</param>
        /// <returns>The matching property name on <see cref="ErxAuditRecord"/>, or null if unrecognized.</returns>
        private static string? ResolveHeaderAlias(string norm)
        {
            // Prescription & Fill
            if (norm.Equals("RXNUMBER", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("RXNO", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("RX", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("MMRXNUMBER", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("MMRXNO", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("MMRX", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("MICROMERCHANTRX", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("MICROMERCHANTRXNUMBER", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("MICROMERCHANTRXNO", StringComparison.OrdinalIgnoreCase))
                return nameof(ErxAuditRecord.RxNumber);

            if (norm.Equals("MMFILLNUMBER", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("MICROMERCHANTFILLNUMBER", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("FILLNUMBER", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("FILLNO", StringComparison.OrdinalIgnoreCase))
                return nameof(ErxAuditRecord.MmFillNumber);

            if (norm.Equals("ERXRXNUMBER", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("ERXRXNO", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("ERXRX", StringComparison.OrdinalIgnoreCase))
                return nameof(ErxAuditRecord.ErxRxNumber);

            // Dates
            if (norm.Equals("MMORDERDATE", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("MICROMERCHANTORDERDATE", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("ORDERDATE", StringComparison.OrdinalIgnoreCase))
                return nameof(ErxAuditRecord.MmOrderDate);

            if (norm.Equals("ERXORDERDATE", StringComparison.OrdinalIgnoreCase))
                return nameof(ErxAuditRecord.ErxOrderDate);

            if (norm.Equals("ERXEFFECTIVEDATE", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("EFFECTIVEDATE", StringComparison.OrdinalIgnoreCase))
                return nameof(ErxAuditRecord.ErxEffectiveDate);

            if (norm.Equals("MMDATEFILLED", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("MICROMERCHANTDATEFILLED", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("DATEFILLED", StringComparison.OrdinalIgnoreCase))
                return nameof(ErxAuditRecord.MmDateFilled);

            if (norm.Equals("MMTIMEFILLED", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("MICROMERCHANTTIMEFILLED", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("TIMEFILLED", StringComparison.OrdinalIgnoreCase))
                return nameof(ErxAuditRecord.MmTimeFilled);

            if (norm.Equals("PICKUPDATE", StringComparison.OrdinalIgnoreCase))
                return nameof(ErxAuditRecord.PickupDate);

            if (norm.Equals("ERXRECEIVEDDATEANDTIME", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("ERXRECEIVEDDATETIME", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("ERXRECEIVEDDATE", StringComparison.OrdinalIgnoreCase))
                return nameof(ErxAuditRecord.ErxReceivedDateTime);

            // Payment & Insurance
            if (norm.Equals("PAYMENTMETHOD", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("PAYMETHOD", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("PAYMENT", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("MMPAYMENTMETHOD", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("MMPAYMENT", StringComparison.OrdinalIgnoreCase))
                return nameof(ErxAuditRecord.PaymentMethod);

            if (norm.Equals("INSCODE", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("INSURANCECODE", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("INSCOD", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("INS", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("MMINSCODE", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("MMINSURANCECODE", StringComparison.OrdinalIgnoreCase))
                return nameof(ErxAuditRecord.InsCode);

            if (norm.Equals("BINNO", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("BIN_NO", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("BIN", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("INSURANCEBIN", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("INSBIN", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("BINNUMBER", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("MMBINNO", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("MMBIN", StringComparison.OrdinalIgnoreCase))
                return nameof(ErxAuditRecord.BinNo);

            if (norm.Equals("TOTALINSPAID", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("TOTALAMOUNTPAID", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("TOTALINSAMOUNTPAID", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("TOTALPAID", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("INSPAID", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("MMTOTALINSPAID", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("MMTOTALPAID", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("AMOUNTPAID", StringComparison.OrdinalIgnoreCase))
                return nameof(ErxAuditRecord.TotalInsPaid);

            // Patient
            if (norm.Equals("MMPATIENTNAME", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("MICROMERCHANTPATIENTNAME", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("PATIENTNAME", StringComparison.OrdinalIgnoreCase))
                return nameof(ErxAuditRecord.MmPatientName);

            if (norm.Equals("ERXPATIENTNAME", StringComparison.OrdinalIgnoreCase))
                return nameof(ErxAuditRecord.ErxPatientName);

            if (norm.Equals("MMPATIENTDOB", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("MICROMERCHANTPATIENTDOB", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("PATIENTDOB", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("DOB", StringComparison.OrdinalIgnoreCase))
                return nameof(ErxAuditRecord.MmPatientDob);

            if (norm.Equals("ERXPATIENTDOB", StringComparison.OrdinalIgnoreCase))
                return nameof(ErxAuditRecord.ErxPatientDob);

            if (norm.Equals("MMPATIENTPHONE", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("MICROMERCHANTPATIENTPHONE", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("PATIENTPHONE", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("PHONE", StringComparison.OrdinalIgnoreCase))
                return nameof(ErxAuditRecord.MmPatientPhone);

            if (norm.Equals("ERXPATIENTPHONE", StringComparison.OrdinalIgnoreCase))
                return nameof(ErxAuditRecord.ErxPatientPhone);

            if (norm.Equals("MMPATIENTADDRESS", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("MICROMERCHANTPATIENTADDRESS", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("PATIENTADDRESS", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("ADDRESS", StringComparison.OrdinalIgnoreCase))
                return nameof(ErxAuditRecord.MmPatientAddress);

            if (norm.Equals("ERXPATIENTADDRESS", StringComparison.OrdinalIgnoreCase))
                return nameof(ErxAuditRecord.ErxPatientAddress);

            // Prescriber
            if (norm.Equals("MMPRESCRIBERNAME", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("MICROMERCHANTPRESCRIBERNAME", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("PRESCRIBERNAME", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("DOCTORNAME", StringComparison.OrdinalIgnoreCase))
                return nameof(ErxAuditRecord.MmPrescriberName);

            if (norm.Equals("ERXPRESCRIBERNAME", StringComparison.OrdinalIgnoreCase))
                return nameof(ErxAuditRecord.ErxPrescriberName);

            if (norm.Equals("SUPERVISORNAME", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("SUPERVISOR", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("SUPERVISINGPHYSICIAN", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("SUPERVISINGPHYSICIANNAME", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("SUPERVISINGDOCTOR", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("SUPERVISINGDR", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("SUPERVISORPHYSICIAN", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("SUPERVISINGPROVIDER", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("SUPERVISINGPROVIDERNAME", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("COLLABORATINGPHYSICIAN", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("ERXSUPERVISORNAME", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("MMSUPERVISORNAME", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("SUPERVISINGMD", StringComparison.OrdinalIgnoreCase))
                return nameof(ErxAuditRecord.SupervisorName);

            if (norm.Equals("MMPRESCRIBERNPI", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("MICROMERCHANTPRESCRIBERNPI", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("PRESCRIBERNPI", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("NPI", StringComparison.OrdinalIgnoreCase))
                return nameof(ErxAuditRecord.MmPrescriberNpi);

            if (norm.Equals("ERXPRESCRIBERNPI", StringComparison.OrdinalIgnoreCase))
                return nameof(ErxAuditRecord.ErxPrescriberNpi);

            if (norm.Equals("MMPRESCRIBERDEA", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("MICROMERCHANTPRESCRIBERDEA", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("PRESCRIBERDEA", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("DEA", StringComparison.OrdinalIgnoreCase))
                return nameof(ErxAuditRecord.MmPrescriberDea);

            if (norm.Equals("ERXPRESCRIBERDEA", StringComparison.OrdinalIgnoreCase))
                return nameof(ErxAuditRecord.ErxPrescriberDea);

            // Drug & NDC & Quantity
            if (norm.Equals("MMDISPENSEDDRUGNAME", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("MICROMERCHANTDISPENSEDDRUGNAME", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("MMDRUGNAME", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("DISPENSEDDRUGNAME", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("DRUGNAME", StringComparison.OrdinalIgnoreCase))
                return nameof(ErxAuditRecord.MmDispensedDrugName);

            if (norm.Equals("ERXDRUGNAME", StringComparison.OrdinalIgnoreCase))
                return nameof(ErxAuditRecord.ErxDrugName);

            if (norm.Equals("MMPOTENCYUNITCODE", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("MICROMERCHANTPOTENCYUNITCODE", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("POTENCYUNITCODE", StringComparison.OrdinalIgnoreCase))
                return nameof(ErxAuditRecord.MmPotencyUnitCode);

            if (norm.Equals("ERXPOTENCYUNITCODE", StringComparison.OrdinalIgnoreCase))
                return nameof(ErxAuditRecord.ErxPotencyUnitCode);

            if (norm.Equals("MMDISPENSEDNDC", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("MICROMERCHANTDISPENSEDNDC", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("MMNDC", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("DISPENSEDNDC", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("NDC", StringComparison.OrdinalIgnoreCase))
                return nameof(ErxAuditRecord.MmDispensedNdc);

            if (norm.Equals("ERXNDC", StringComparison.OrdinalIgnoreCase))
                return nameof(ErxAuditRecord.ErxNdc);

            if (norm.Equals("MMQTYORD", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("MICROMERCHANTQTYORD", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("QTYORD", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("QUANTITYORDERED", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("MMQUANTITYORDERED", StringComparison.OrdinalIgnoreCase))
                return nameof(ErxAuditRecord.MmQtyOrd);

            if (norm.Equals("ERXQTYORD", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("ERXQUANTITYORDERED", StringComparison.OrdinalIgnoreCase))
                return nameof(ErxAuditRecord.ErxQtyOrd);

            if (norm.Equals("MMQTYDISPENSED", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("MICROMERCHANTQTYDISPENSED", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("QTYDISPENSED", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("QUANTITYDISPENSED", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("MMQUANTITYDISPENSED", StringComparison.OrdinalIgnoreCase))
                return nameof(ErxAuditRecord.MmQtyDispensed);

            if (norm.Equals("ERXQTYDISPENSED", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("ERXQUANTITYDISPENSED", StringComparison.OrdinalIgnoreCase))
                return nameof(ErxAuditRecord.ErxQtyDispensed);

            if (norm.Equals("MMDISPENSEDDAYSSUPPLY", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("MICROMERCHANTDISPENSEDDAYSSUPPLY", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("MMDAYSSUPPLY", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("DAYSSUPPLY", StringComparison.OrdinalIgnoreCase))
                return nameof(ErxAuditRecord.MmDispensedDaysSupply);

            if (norm.Equals("ERXDAYSSUPPLY", StringComparison.OrdinalIgnoreCase))
                return nameof(ErxAuditRecord.ErxDaysSupply);

            if (norm.Equals("MMDISPENSEDSIG", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("MICROMERCHANTDISPENSEDSIG", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("MMSIG", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("DISPENSEDSIG", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("SIG", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("DIRECTIONS", StringComparison.OrdinalIgnoreCase))
                return nameof(ErxAuditRecord.MmDispensedSig);

            if (norm.Equals("ERXSIG", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("ERXDIRECTIONS", StringComparison.OrdinalIgnoreCase))
                return nameof(ErxAuditRecord.ErxSig);

            // Notes & Misc
            if (norm.Equals("TELEPHONENOTES", StringComparison.OrdinalIgnoreCase))
                return nameof(ErxAuditRecord.TelephoneNotes);

            if (norm.Equals("TELEPHONENOTESDATECREATED", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("TELEPHONENOTESDATE", StringComparison.OrdinalIgnoreCase))
                return nameof(ErxAuditRecord.TelephoneNotesDate);

            if (norm.Equals("ERXNOTES", StringComparison.OrdinalIgnoreCase))
                return nameof(ErxAuditRecord.ErxNotes);

            if (norm.Equals("ERXNOTESDATECREATED", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("ERXNOTESDATE", StringComparison.OrdinalIgnoreCase))
                return nameof(ErxAuditRecord.ErxNotesDate);

            if (norm.Equals("ERXNOTESCREATEDBY", StringComparison.OrdinalIgnoreCase))
                return nameof(ErxAuditRecord.ErxNotesCreatedBy);

            if (norm.Equals("MMDAWCODE", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("MICROMERCHANTDAWCODE", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("DAWCODE", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("DAW", StringComparison.OrdinalIgnoreCase))
                return nameof(ErxAuditRecord.MmDawCode);

            if (norm.Equals("ERXDAWCODE", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("ERXDAW", StringComparison.OrdinalIgnoreCase))
                return nameof(ErxAuditRecord.ErxDawCode);

            if (norm.Equals("MMREFILLSPHARMACISTENTERED", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("MICROMERCHANTREFILLSPHARMACISTENTERED", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("MMREFILLS", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("MMREFILL", StringComparison.OrdinalIgnoreCase))
                return nameof(ErxAuditRecord.MmRefillsPharmacistEntered);

            if (norm.Equals("ERXREFILL", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("ERXREFILLS", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("ERXREFILLSPHARMACISTENTERED", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("REFILL", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("REFILLS", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("REFILLSORIGINALLYPRESCRIBERSENT", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("REFILLSORIGINALLYSENT", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("REFILLSORIGINALLYPRESCRIBED", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("REFILLSPRESCRIBERSENT", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("REFILLSPRESCRIBED", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("ERXREFILLSORIGINALLYPRESCRIBERSENT", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("ORIGINALLYPRESCRIBEDREFILLS", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("ORIGINALREFILLS", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("PRESCRIBERREFILLS", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("PRESCRIBERSENTREFILLS", StringComparison.OrdinalIgnoreCase) ||
                (norm.Contains("REFILL", StringComparison.OrdinalIgnoreCase) && norm.Contains("PRESCRIBER", StringComparison.OrdinalIgnoreCase)) ||
                (norm.Contains("REFILL", StringComparison.OrdinalIgnoreCase) && norm.Contains("ORIGIN", StringComparison.OrdinalIgnoreCase)))
                return nameof(ErxAuditRecord.ErxRefill);


            if (norm.Equals("MMRPHNOTES", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("MICROMERCHANTRPHNOTES", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("RPHNOTES", StringComparison.OrdinalIgnoreCase))
                return nameof(ErxAuditRecord.MmRphNotes);

            if (norm.Equals("MMSIGNATURECAPTUREDYN", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("MMSIGNATURECAPTURED", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("MICROMERCHANTSIGNATURECAPTUREDYN", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("MICROMERCHANTSIGNATURECAPTURED", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("SIGNATURECAPTURED", StringComparison.OrdinalIgnoreCase))
                return nameof(ErxAuditRecord.MmSignatureCaptured);

            if (norm.Equals("FOLLOWUPTAG", StringComparison.OrdinalIgnoreCase))
                return nameof(ErxAuditRecord.FollowUpTag);

            if (norm.Equals("MMDELIVERY", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("MICROMERCHANTDELIVERY", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("DELIVERY", StringComparison.OrdinalIgnoreCase))
                return nameof(ErxAuditRecord.MmDelivery);

            if (norm.Equals("MMRXORIGIN", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("MICROMERCHANTRXORIGIN", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("RXORIGIN", StringComparison.OrdinalIgnoreCase))
                return nameof(ErxAuditRecord.MmRxOrigin);

            if (norm.Equals("MMCOPAY", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("MICROMERCHANTCOPAY", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("COPAY", StringComparison.OrdinalIgnoreCase))
                return nameof(ErxAuditRecord.MmCopay);

            if (norm.Equals("DURREASONCD", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("DURREASON", StringComparison.OrdinalIgnoreCase))
                return nameof(ErxAuditRecord.DurReasonCd);

            if (norm.Equals("DURPROFCD", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("DURPROF", StringComparison.OrdinalIgnoreCase))
                return nameof(ErxAuditRecord.DurProfCd);

            if (norm.Equals("DURRESULTCODE", StringComparison.OrdinalIgnoreCase) ||
                norm.Equals("DURRESULT", StringComparison.OrdinalIgnoreCase))
                return nameof(ErxAuditRecord.DurResultCode);

            if (norm.Equals("MESSAGE", StringComparison.OrdinalIgnoreCase))
                return nameof(ErxAuditRecord.Message);

            return null;
        }

        /// <summary>
        /// Maps a single row of raw string data into a strongly-typed <see cref="ErxAuditRecord"/> entity.
        /// 
        /// Purpose:
        /// - Uses the prebuilt column mapping dictionary to locate destination properties on the record.
        /// - Calls <see cref="SetPropertyValue"/> to handle reflection-based type conversions.
        /// - Attaches the parent BatchId and sets CreatedAt timestamp.
        /// </summary>
        /// <param name="rowData">Dictionary of raw column headers to string cell values for the row.</param>
        /// <param name="columnMap">Precalculated mapping of column headers to entity property names.</param>
        /// <param name="batchId">The audit batch ID to associate with the record.</param>
        /// <returns>A populated <see cref="ErxAuditRecord"/> entity.</returns>
        private static ErxAuditRecord MapRowToRecord(Dictionary<string, string> rowData, Dictionary<string, string> columnMap, Guid batchId)
        {
            var record = new ErxAuditRecord
            {
                BatchId = batchId,
                CreatedAt = DateTime.UtcNow
            };

            var propLookup = typeof(ErxAuditRecord).GetProperties().ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

            foreach (var kvp in rowData)
            {
                if (!columnMap.TryGetValue(kvp.Key, out var propName)) continue;
                if (!propLookup.TryGetValue(propName, out var prop)) continue;

                var rawVal = kvp.Value?.Trim();
                if (string.IsNullOrEmpty(rawVal)) continue;

                try
                {
                    SetPropertyValue(record, prop, rawVal);
                }
                catch
                {
                    // Ignore conversion issue and keep going
                }
            }

            return record;
        }

        /// <summary>
        /// Safely coerces a raw string cell value into the target property type on <see cref="ErxAuditRecord"/>.
        /// 
        /// Purpose:
        /// - Enforces database length constraints for strings (using <see cref="MaxLengthAttribute"/>).
        /// - Parses integers, longs, and decimals with regex cleanup (stripping currency symbols, commas).
        /// - Parses DateTimes across multiple cultures ('en-US', 'en-GB', Invariant) and handles Excel OLE numeric dates.
        /// - Parses TimeSpans and day fractions.
        /// - Parses Booleans from text ('true'/'false', 'Y'/'N', '1'/'0', 'YES'/'NO').
        /// - Prevents data import crashes caused by malformed individual cell inputs.
        /// </summary>
        /// <param name="record">The target <see cref="ErxAuditRecord"/> instance.</param>
        /// <param name="prop">The reflection property metadata.</param>
        /// <param name="rawVal">The raw string value extracted from the file.</param>
        private static void SetPropertyValue(ErxAuditRecord record, System.Reflection.PropertyInfo prop, string rawVal)
        {
            // Never assign database-generated primary keys
            if (prop.Name.Equals(nameof(ErxAuditRecord.RecordId), StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

            if (targetType == typeof(string))
            {
                var maxLenAttr = prop.GetCustomAttribute<MaxLengthAttribute>();
                if (maxLenAttr != null && rawVal.Length > maxLenAttr.Length)
                {
                    rawVal = rawVal.Substring(0, maxLenAttr.Length);
                }
                prop.SetValue(record, rawVal);
            }
            else if (targetType == typeof(int))
            {
                if (int.TryParse(Regex.Replace(rawVal, @"[^\d-]", ""), out var iVal))
                    prop.SetValue(record, iVal);
            }
            else if (targetType == typeof(long))
            {
                if (long.TryParse(Regex.Replace(rawVal, @"[^\d-]", ""), out var lVal))
                    prop.SetValue(record, lVal);
            }
            else if (targetType == typeof(decimal))
            {
                var clean = Regex.Replace(rawVal, @"[^\d.-]", "");
                if (decimal.TryParse(clean, NumberStyles.Any, CultureInfo.InvariantCulture, out var dVal))
                {
                    if (dVal >= -999999999999999m && dVal <= 999999999999999m)
                    {
                        prop.SetValue(record, dVal);
                    }
                }
            }
            else if (targetType == typeof(DateTime))
            {
                var cleanVal = rawVal.Trim();
                if (DateTime.TryParseExact(cleanVal, UsaDateFormats, CultureInfo.GetCultureInfo("en-US"), DateTimeStyles.None, out var dtVal) ||
                    DateTime.TryParse(cleanVal, CultureInfo.GetCultureInfo("en-US"), DateTimeStyles.None, out dtVal) ||
                    DateTime.TryParse(cleanVal, CultureInfo.InvariantCulture, DateTimeStyles.None, out dtVal))
                {
                    prop.SetValue(record, dtVal);
                }
                else if (double.TryParse(cleanVal, NumberStyles.Any, CultureInfo.InvariantCulture, out var oaDate) && oaDate >= 20000 && oaDate <= 100000)
                {
                    try
                    {
                        var dt = DateTime.FromOADate(oaDate);
                        prop.SetValue(record, dt);
                    }
                    catch { }
                }
            }
            else if (targetType == typeof(TimeSpan))
            {
                if (TimeSpan.TryParse(rawVal, out var tsVal))
                {
                    prop.SetValue(record, tsVal);
                }
                else if (double.TryParse(rawVal, NumberStyles.Any, CultureInfo.InvariantCulture, out var dayFraction) && dayFraction >= 0 && dayFraction < 1)
                {
                    try
                    {
                        prop.SetValue(record, TimeSpan.FromDays(dayFraction));
                    }
                    catch { }
                }
                else if (DateTime.TryParse(rawVal, out var timeDt))
                {
                    prop.SetValue(record, timeDt.TimeOfDay);
                }
            }
            else if (targetType == typeof(bool))
            {
                if (bool.TryParse(rawVal, out var bVal))
                    prop.SetValue(record, bVal);
                else if (rawVal == "1" || rawVal.Equals("Y", StringComparison.OrdinalIgnoreCase) || rawVal.Equals("YES", StringComparison.OrdinalIgnoreCase))
                    prop.SetValue(record, true);
                else if (rawVal == "0" || rawVal.Equals("N", StringComparison.OrdinalIgnoreCase) || rawVal.Equals("NO", StringComparison.OrdinalIgnoreCase))
                    prop.SetValue(record, false);
            }
        }
    }
}
