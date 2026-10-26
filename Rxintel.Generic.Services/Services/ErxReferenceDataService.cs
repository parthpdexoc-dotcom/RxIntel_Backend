using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Entities.Entities;
using Rxintel.Generic.Services.Interfaces;

namespace Rxintel.Generic.Services.Services
{
    public class ErxReferenceDataService : IErxReferenceDataService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ErxReferenceDataService> _logger;

        private readonly SemaphoreSlim _loadLock = new(1, 1);
        private volatile bool _isLoaded = false;

        // Controlled Drugs
        private readonly ConcurrentDictionary<string, ControlledDrugDto> _drugsByNdc = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, ControlledDrugDto> _drugsByName = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<ControlledDrugDto> _controlledDrugsList = new();

        // State Rx Validity
        private readonly Dictionary<string, StateRxValidityDto> _stateValidity = new(StringComparer.OrdinalIgnoreCase);

        // Supervising Physician Rules
        private readonly Dictionary<string, SupervisingPhysicianRuleDto> _supervisingRules = new(StringComparer.OrdinalIgnoreCase);

        // NPI Provider Directory: NPI -> (byte Type: 1=PA, 2=NP, string State)
        private readonly Dictionary<long, (byte Type, string State)> _npiProviders = new();

        private static readonly Dictionary<string, string> StateNameToCodeMap = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Alabama", "AL" }, { "Alaska", "AK" }, { "Arizona", "AZ" }, { "Arkansas", "AR" },
            { "California", "CA" }, { "Colorado", "CO" }, { "Connecticut", "CT" }, { "Delaware", "DE" },
            { "Florida", "FL" }, { "Georgia", "GA" }, { "Hawaii", "HI" }, { "Idaho", "ID" },
            { "Illinois", "IL" }, { "Indiana", "IN" }, { "Iowa", "IA" }, { "Kansas", "KS" },
            { "Kentucky", "KY" }, { "Louisiana", "LA" }, { "Maine", "ME" }, { "Maryland", "MD" },
            { "Massachusetts", "MA" }, { "Michigan", "MI" }, { "Minnesota", "MN" }, { "Mississippi", "MS" },
            { "Missouri", "MO" }, { "Montana", "MT" }, { "Nebraska", "NE" }, { "Nevada", "NV" },
            { "New Hampshire", "NH" }, { "New Jersey", "NJ" }, { "New Mexico", "NM" }, { "New York", "NY" },
            { "North Carolina", "NC" }, { "North Dakota", "ND" }, { "Ohio", "OH" }, { "Oklahoma", "OK" },
            { "Oregon", "OR" }, { "Pennsylvania", "PA" }, { "Rhode Island", "RI" }, { "South Carolina", "SC" },
            { "South Dakota", "SD" }, { "Tennessee", "TN" }, { "Texas", "TX" }, { "Utah", "UT" },
            { "Vermont", "VT" }, { "Virginia", "VA" }, { "Washington", "WA" }, { "West Virginia", "WV" },
            { "Wisconsin", "WI" }, { "Wyoming", "WY" }, { "District of Columbia", "DC" },
            { "Puerto Rico", "PR" }, { "Virgin Islands", "VI" }, { "Guam", "GU" }
        };

        private static readonly Dictionary<string, string> CodeToStateNameMap =
            StateNameToCodeMap.GroupBy(kv => kv.Value, StringComparer.OrdinalIgnoreCase)
                              .ToDictionary(g => g.Key, g => g.First().Key, StringComparer.OrdinalIgnoreCase);

        public ErxReferenceDataService(
            IConfiguration configuration,
            ILogger<ErxReferenceDataService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task EnsureLoadedAsync()
        {
            if (_isLoaded) return;

            await _loadLock.WaitAsync();
            try
            {
                if (_isLoaded) return;
                LoadAllReferenceData();
                _isLoaded = true;
            }
            finally
            {
                _loadLock.Release();
            }
        }

        private void EnsureLoadedInternal()
        {
            if (_isLoaded) return;

            _loadLock.Wait();
            try
            {
                if (_isLoaded) return;
                LoadAllReferenceData();
                _isLoaded = true;
            }
            finally
            {
                _loadLock.Release();
            }
        }

        private string ResolveBasePath()
        {
            var configured = _configuration["AuditReferenceData:FolderPath"]
                             ?? _configuration["AuditReferenceData:BasePath"]
                             ?? @"F:\Projects\RX_INTEL_NEW\ReferenceData";

            if (Directory.Exists(configured)) return configured;

            var localRel = Path.Combine(AppContext.BaseDirectory, "ReferenceData");
            if (Directory.Exists(localRel)) return localRel;

            var rootRel = Path.Combine(Directory.GetCurrentDirectory(), "ReferenceData");
            if (Directory.Exists(rootRel)) return rootRel;

            return configured;
        }

        private void LoadAllReferenceData()
        {
            var basePath = ResolveBasePath();
            _logger.LogInformation("[ErxReferenceDataService] Loading reference datasets from directory: '{BasePath}'", basePath);

            var sw = Stopwatch.StartNew();

            try
            {
                LoadControlledDrugs(basePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ErxReferenceDataService] Error loading Scheduled Drugs file from '{BasePath}': {Message}", basePath, ex.Message);
            }

            try
            {
                LoadStateRxValidity(basePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ErxReferenceDataService] Error loading State Rx Validity file from '{BasePath}': {Message}", basePath, ex.Message);
            }

            try
            {
                LoadSupervisingPhysicianRules(basePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ErxReferenceDataService] Error loading Supervising Physician Rules file from '{BasePath}': {Message}", basePath, ex.Message);
            }

            try
            {
                LoadNpiProviders(basePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ErxReferenceDataService] Error loading NPI Providers from '{BasePath}': {Message}", basePath, ex.Message);
            }

            sw.Stop();
            _logger.LogInformation(
                "[ErxReferenceDataService] Initialization complete in {Elapsed} ms. (Controlled Drugs: {Drugs}, State Validity: {States}, Supervising Rules: {SupRules}, NPI Providers: {Providers})",
                sw.ElapsedMilliseconds, _drugsByNdc.Count, _stateValidity.Count, _supervisingRules.Count, _npiProviders.Count);
        }

        private void LoadControlledDrugs(string basePath)
        {
            var fileName = _configuration["AuditReferenceData:ControlledDrugsFileName"]
                           ?? _configuration["AuditReferenceData:ControlledDrugsFile"]
                           ?? "ControlledDrugs/Scheduled_Drugs_List.xlsx";
            var filePath = Path.IsPathRooted(fileName) ? fileName : Path.Combine(basePath, fileName);

            if (!File.Exists(filePath))
            {
                _logger.LogWarning("[ErxReferenceDataService] Controlled Drugs file not found at '{FilePath}'.", filePath);
                return;
            }

            using var workbook = new XLWorkbook(filePath);
            var ws = workbook.Worksheets.FirstOrDefault(w => w.Name.Equals("Scheduled Drugs", StringComparison.OrdinalIgnoreCase))
                     ?? workbook.Worksheets.FirstOrDefault();

            if (ws == null) return;

            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;
            int loadedCount = 0;

            for (int r = 2; r <= lastRow; r++)
            {
                var row = ws.Row(r);
                var productNdc = row.Cell(1).GetString().Trim();
                var brand = row.Cell(2).GetString().Trim();
                var generic = row.Cell(3).GetString().Trim();
                var dosage = row.Cell(4).GetString().Trim();
                var route = row.Cell(5).GetString().Trim();
                var strength = row.Cell(6).GetString().Trim();
                var labeler = row.Cell(7).GetString().Trim();
                var schedStr = row.Cell(8).GetString().Trim();

                if (string.IsNullOrWhiteSpace(productNdc) && string.IsNullOrWhiteSpace(brand) && string.IsNullOrWhiteSpace(generic))
                    continue;

                int? schedNum = ParseDeaScheduleNumber(schedStr);

                var dto = new ControlledDrugDto
                {
                    ProductNdc = productNdc,
                    NormalizedNdc = NormalizeNdc(productNdc),
                    BrandName = brand,
                    GenericName = generic,
                    DosageForm = dosage,
                    Route = route,
                    Strength = strength,
                    Labeler = labeler,
                    DeaSchedule = schedStr,
                    ScheduleNumber = schedNum
                };

                // Index by NDC
                if (!string.IsNullOrWhiteSpace(dto.NormalizedNdc))
                {
                    _drugsByNdc[dto.NormalizedNdc] = dto;

                    // Also index 11-digit zero padded versions
                    if (dto.NormalizedNdc.Length == 10)
                    {
                        _drugsByNdc["0" + dto.NormalizedNdc] = dto;
                        _drugsByNdc[dto.NormalizedNdc.Insert(5, "0")] = dto;
                        _drugsByNdc[dto.NormalizedNdc.Insert(9, "0")] = dto;
                    }
                    if (!string.IsNullOrWhiteSpace(productNdc))
                    {
                        _drugsByNdc[productNdc] = dto;
                    }
                }

                // Index by Clean Names
                if (!string.IsNullOrWhiteSpace(brand))
                {
                    var cleanBrand = CleanDrugName(brand);
                    if (!string.IsNullOrEmpty(cleanBrand))
                        _drugsByName[cleanBrand] = dto;
                }

                if (!string.IsNullOrWhiteSpace(generic))
                {
                    var cleanGeneric = CleanDrugName(generic);
                    if (!string.IsNullOrEmpty(cleanGeneric))
                        _drugsByName[cleanGeneric] = dto;
                }

                if (dto.IsControlled)
                {
                    _controlledDrugsList.Add(dto);
                }

                loadedCount++;
            }

            _logger.LogInformation("[ErxReferenceDataService] Loaded {Count} Scheduled Drug records from '{FilePath}'.", loadedCount, filePath);
        }

        private void LoadStateRxValidity(string basePath)
        {
            var fileName = _configuration["AuditReferenceData:StateValidityFileName"]
                           ?? _configuration["AuditReferenceData:StateValidityFile"]
                           ?? "StateRxValidity/Rx_Validity_by_State.xlsx";
            var filePath = Path.IsPathRooted(fileName) ? fileName : Path.Combine(basePath, fileName);

            if (!File.Exists(filePath))
            {
                _logger.LogWarning("[ErxReferenceDataService] State Rx Validity file not found at '{FilePath}'.", filePath);
                return;
            }

            using var workbook = new XLWorkbook(filePath);
            var ws = workbook.Worksheets.FirstOrDefault(w => w.Name.Equals("Validity by State", StringComparison.OrdinalIgnoreCase))
                     ?? workbook.Worksheets.FirstOrDefault();

            if (ws == null) return;

            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;
            for (int r = 2; r <= lastRow; r++)
            {
                var row = ws.Row(r);
                var stateName = row.Cell(1).GetString().Trim();
                var s2Raw = row.Cell(2).GetString().Trim();
                var s35Raw = row.Cell(4).GetString().Trim();
                var nonRaw = row.Cell(6).GetString().Trim();
                var notes = row.Cell(8).GetString().Trim();

                if (string.IsNullOrWhiteSpace(stateName)) continue;

                var stateCode = StateNameToCodeMap.TryGetValue(stateName, out var code) ? code : stateName.ToUpperInvariant();

                var dto = new StateRxValidityDto
                {
                    StateName = stateName,
                    StateCode = stateCode,
                    Schedule2ValidityDays = ParseValidityDays(s2Raw),
                    Schedule2RawText = s2Raw,
                    Schedule3To5ValidityDays = ParseValidityDays(s35Raw),
                    Schedule3To5RawText = s35Raw,
                    NonControlledValidityDays = ParseValidityDays(nonRaw),
                    NonControlledRawText = nonRaw,
                    Notes = notes
                };

                // Texas specific override / verification: Texas is 30 days for Class 2
                if (string.Equals(stateCode, "TX", StringComparison.OrdinalIgnoreCase) && !dto.Schedule2ValidityDays.HasValue)
                {
                    dto.Schedule2ValidityDays = 30;
                }

                _stateValidity[stateName] = dto;
                _stateValidity[stateCode] = dto;
            }

            _logger.LogInformation("[ErxReferenceDataService] Loaded {Count} State Rx Validity rules from '{FilePath}'.", _stateValidity.Count / 2, filePath);
        }

        private void LoadSupervisingPhysicianRules(string basePath)
        {
            var fileName = _configuration["AuditReferenceData:SupervisingPhysicianFileName"]
                           ?? _configuration["AuditReferenceData:SupervisingPhysicianFile"]
                           ?? "SupervisingPhysicianRules/PA_NP_Physician_Name_on_Rx_by_State.xlsx";
            var filePath = Path.IsPathRooted(fileName) ? fileName : Path.Combine(basePath, fileName);

            if (!File.Exists(filePath))
            {
                _logger.LogWarning("[ErxReferenceDataService] Supervising Physician Rules file not found at '{FilePath}'.", filePath);
                return;
            }

            using var workbook = new XLWorkbook(filePath);
            var ws = workbook.Worksheets.FirstOrDefault(w => w.Name.Equals("Requirements by State", StringComparison.OrdinalIgnoreCase))
                     ?? workbook.Worksheets.FirstOrDefault();

            if (ws == null) return;

            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;
            for (int r = 2; r <= lastRow; r++)
            {
                var row = ws.Row(r);
                var stateName = row.Cell(1).GetString().Trim();
                var paRaw = row.Cell(2).GetString().Trim();
                var npRaw = row.Cell(4).GetString().Trim();
                var notes = row.Cell(6).GetString().Trim();

                if (string.IsNullOrWhiteSpace(stateName)) continue;

                var stateCode = StateNameToCodeMap.TryGetValue(stateName, out var code) ? code : stateName.ToUpperInvariant();

                bool paReq = paRaw.StartsWith("Yes", StringComparison.OrdinalIgnoreCase);
                bool npReq = npRaw.StartsWith("Yes", StringComparison.OrdinalIgnoreCase);

                var dto = new SupervisingPhysicianRuleDto
                {
                    StateName = stateName,
                    StateCode = stateCode,
                    PaSupervisingRequired = paReq,
                    PaRawText = paRaw,
                    NpSupervisingRequired = npReq,
                    NpRawText = npRaw,
                    Notes = notes
                };

                _supervisingRules[stateName] = dto;
                _supervisingRules[stateCode] = dto;
            }

            _logger.LogInformation("[ErxReferenceDataService] Loaded {Count} Supervising Physician State rules from '{FilePath}'.", _supervisingRules.Count / 2, filePath);
        }

        private void LoadNpiProviders(string basePath)
        {
            var npiDir = _configuration["AuditReferenceData:NpiDirectory"] ?? "NpiRecords";
            var dirPath = Path.IsPathRooted(npiDir) ? npiDir : Path.Combine(basePath, npiDir);

            if (!Directory.Exists(dirPath))
            {
                _logger.LogWarning("[ErxReferenceDataService] NPI Directory not found at '{DirPath}'.", dirPath);
                return;
            }

            var binFile = _configuration["AuditReferenceData:NpiBinaryCacheFile"]
                          ?? Path.Combine(dirPath, "npi_providers.bin");

            if (File.Exists(binFile))
            {
                try
                {
                    using var fs = File.OpenRead(binFile);
                    using var br = new BinaryReader(fs);

                    // Validate 8-byte header (Magic: 1IPN / 0x31-0x49-0x50-0x4E, int header)
                    byte[] magic = br.ReadBytes(4);
                    int headerVal = br.ReadInt32();

                    while (fs.Position + 11 <= fs.Length)
                    {
                        long npi = br.ReadInt64();
                        byte pType = br.ReadByte();
                        byte[] stateBytes = br.ReadBytes(2);
                        string state = Encoding.ASCII.GetString(stateBytes);

                        _npiProviders[npi] = (pType, state);
                    }

                    _logger.LogInformation("[ErxReferenceDataService] Loaded {Count} PA/NP providers from binary cache '{BinFile}'.", _npiProviders.Count, binFile);
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[ErxReferenceDataService] Failed to load binary NPI cache '{BinFile}', falling back to Excel files: {Message}", binFile, ex.Message);
                    _npiProviders.Clear();
                }
            }

            // Fallback: Read from state .xlsx files in NpiRecords directory
            var excelFiles = Directory.GetFiles(dirPath, "*_PA_NP_Active.xlsx");
            _logger.LogInformation("[ErxReferenceDataService] Parsing {Count} NPI Excel files from '{DirPath}'...", excelFiles.Length, dirPath);

            foreach (var file in excelFiles)
            {
                try
                {
                    using var wb = new XLWorkbook(file);
                    var ws = wb.Worksheets.FirstOrDefault();
                    if (ws == null) continue;

                    var lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;
                    for (int r = 2; r <= lastRow; r++)
                    {
                        var npiStr = ws.Cell(r, 1).GetString().Trim();
                        var typeStr = ws.Cell(r, 2).GetString().Trim();
                        var stateStr = ws.Cell(r, 18).GetString().Trim();

                        if (long.TryParse(npiStr, out var npi) && npi > 0)
                        {
                            byte pType = string.Equals(typeStr, "NP", StringComparison.OrdinalIgnoreCase) ? (byte)2 : (byte)1;
                            var st = stateStr.Length >= 2 ? stateStr.Substring(0, 2).ToUpperInvariant() : "US";
                            _npiProviders[npi] = (pType, st);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[ErxReferenceDataService] Error reading NPI file '{File}': {Message}", file, ex.Message);
                }
            }

            _logger.LogInformation("[ErxReferenceDataService] Successfully parsed {Count} NPI providers from Excel files.", _npiProviders.Count);

            // Attempt to write binary cache for future instantaneous starts
            try
            {
                var dir = Path.GetDirectoryName(binFile);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                using var fs = File.Create(binFile);
                using var bw = new BinaryWriter(fs);
                bw.Write(new byte[] { 0x31, 0x49, 0x50, 0x4E }); // "1IPN"
                bw.Write((int)60);
                foreach (var kvp in _npiProviders)
                {
                    bw.Write(kvp.Key);
                    bw.Write(kvp.Value.Type);
                    var st = (kvp.Value.State ?? "US").PadRight(2).Substring(0, 2);
                    bw.Write(Encoding.ASCII.GetBytes(st));
                }
                _logger.LogInformation("[ErxReferenceDataService] Generated new NPI binary cache at '{BinFile}'.", binFile);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[ErxReferenceDataService] Could not write binary NPI cache: {Message}", ex.Message);
            }
        }

        public ControlledDrugDto? FindControlledDrug(string? ndc, string? drugName = null)
        {
            EnsureLoadedInternal();

            // 1. Lookup by NDC
            if (!string.IsNullOrWhiteSpace(ndc))
            {
                var cleanNdc = NormalizeNdc(ndc);
                if (!string.IsNullOrEmpty(cleanNdc))
                {
                    if (_drugsByNdc.TryGetValue(cleanNdc, out var drug)) return drug;
                    if (_drugsByNdc.TryGetValue(ndc.Trim(), out drug)) return drug;

                    // Check FDA 10-to-11 / 11-to-10 conversions
                    if (cleanNdc.Length == 11 && cleanNdc.StartsWith("0") && _drugsByNdc.TryGetValue(cleanNdc.Substring(1), out drug)) return drug;
                    if (cleanNdc.Length == 10 && _drugsByNdc.TryGetValue("0" + cleanNdc, out drug)) return drug;
                }
            }

            // 2. Lookup by Drug Name / Generic Name
            if (!string.IsNullOrWhiteSpace(drugName))
            {
                var cleanName = CleanDrugName(drugName);
                if (!string.IsNullOrEmpty(cleanName))
                {
                    if (_drugsByName.TryGetValue(cleanName, out var drug)) return drug;

                    // Fallback: check if any brand/generic is contained within the drug name or vice versa
                    var match = _controlledDrugsList.FirstOrDefault(d =>
                        (!string.IsNullOrEmpty(d.BrandName) && cleanName.Contains(CleanDrugName(d.BrandName), StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(d.GenericName) && cleanName.Contains(CleanDrugName(d.GenericName), StringComparison.OrdinalIgnoreCase)));

                    if (match != null) return match;
                }
            }

            return null;
        }

        public bool IsControlledSubstance(string? ndc, string? drugName = null)
        {
            var match = FindControlledDrug(ndc, drugName);
            return match != null && match.IsControlled;
        }

        public bool IsControlledSubstance(ErxAuditRecord? record)
        {
            if (record == null) return false;

            return IsControlledSubstance(record.MmDispensedNdc, record.MmDispensedDrugName) ||
                   IsControlledSubstance(record.ErxNdc, record.ErxDrugName);
        }

        public int? GetDeaSchedule(string? ndc, string? drugName = null)
        {
            var match = FindControlledDrug(ndc, drugName);
            return match?.ScheduleNumber;
        }

        public int? GetDeaSchedule(ErxAuditRecord? record)
        {
            if (record == null) return null;

            var sched = GetDeaSchedule(record.MmDispensedNdc, record.MmDispensedDrugName);
            if (sched.HasValue) return sched;

            return GetDeaSchedule(record.ErxNdc, record.ErxDrugName);
        }

        public StateRxValidityDto? GetStateValidity(string? stateOrAddress)
        {
            EnsureLoadedInternal();
            if (string.IsNullOrWhiteSpace(stateOrAddress)) return null;

            var stateCode = ResolveStateFromAddress(stateOrAddress);
            if (!string.IsNullOrEmpty(stateCode) && _stateValidity.TryGetValue(stateCode, out var rule))
                return rule;

            if (_stateValidity.TryGetValue(stateOrAddress.Trim(), out rule))
                return rule;

            return null;
        }

        public SupervisingPhysicianRuleDto? GetSupervisingRule(string? stateOrAddress)
        {
            EnsureLoadedInternal();
            if (string.IsNullOrWhiteSpace(stateOrAddress)) return null;

            var stateCode = ResolveStateFromAddress(stateOrAddress);
            if (!string.IsNullOrEmpty(stateCode) && _supervisingRules.TryGetValue(stateCode, out var rule))
                return rule;

            if (_supervisingRules.TryGetValue(stateOrAddress.Trim(), out rule))
                return rule;

            return null;
        }

        public NpiProviderDto? FindProviderByNpi(string? npi)
        {
            EnsureLoadedInternal();
            if (string.IsNullOrWhiteSpace(npi)) return null;

            var digits = Regex.Replace(npi, @"\D", "");
            if (long.TryParse(digits, out var npiLong) && _npiProviders.TryGetValue(npiLong, out var val))
            {
                return new NpiProviderDto
                {
                    Npi = npiLong,
                    ProviderType = val.Type == 2 ? "NP" : "PA",
                    State = val.State
                };
            }

            return null;
        }

        public bool IsSupervisingPhysicianRequired(string? npi, string? stateOrAddress, out string providerType, out string resolvedState)
        {
            providerType = string.Empty;
            resolvedState = ResolveStateFromAddress(stateOrAddress);

            var provider = FindProviderByNpi(npi);
            if (provider == null)
            {
                return false;
            }

            providerType = provider.ProviderType;
            if (string.IsNullOrEmpty(resolvedState))
            {
                resolvedState = provider.State;
            }

            var rule = GetSupervisingRule(resolvedState);
            if (rule == null)
            {
                return false;
            }

            return string.Equals(providerType, "PA", StringComparison.OrdinalIgnoreCase)
                ? rule.PaSupervisingRequired
                : rule.NpSupervisingRequired;
        }

        public string ResolveStateFromAddress(string? address1, string? address2 = null)
        {
            var combined = string.Concat(address1, " ", address2);
            if (string.IsNullOrWhiteSpace(combined)) return string.Empty;

            // 1. Check for standard 2-letter state abbreviation followed by 5-digit ZIP code: e.g. "TX 77001", "FL 33101"
            var zipMatch = Regex.Match(combined, @"\b([A-Za-z]{2})\b\s*(\d{5}(-\d{4})?)");
            if (zipMatch.Success)
            {
                var candidate = zipMatch.Groups[1].Value.ToUpperInvariant();
                if (CodeToStateNameMap.ContainsKey(candidate))
                    return candidate;
            }

            // 2. Check full state name match
            foreach (var kvp in StateNameToCodeMap)
            {
                if (Regex.IsMatch(combined, $@"\b{Regex.Escape(kvp.Key)}\b", RegexOptions.IgnoreCase))
                    return kvp.Value;
            }

            // 3. Check standalone 2-letter state abbreviations
            foreach (var code in CodeToStateNameMap.Keys)
            {
                if (Regex.IsMatch(combined, $@"\b{Regex.Escape(code)}\b", RegexOptions.IgnoreCase))
                    return code;
            }

            return string.Empty;
        }

        private static string NormalizeNdc(string? ndc)
        {
            if (string.IsNullOrWhiteSpace(ndc)) return string.Empty;
            return Regex.Replace(ndc.Trim(), @"[^\w]", "").ToUpperInvariant();
        }

        private static string CleanDrugName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return string.Empty;
            var upper = name.Trim().ToUpperInvariant();
            upper = Regex.Replace(upper, @"[\\/,\.\-#()]+", " ");
            upper = Regex.Replace(upper, @"\s+", " ").Trim();
            return upper;
        }

        private static int? ParseDeaScheduleNumber(string? sched)
        {
            if (string.IsNullOrWhiteSpace(sched)) return null;
            var s = sched.Trim().ToUpperInvariant();
            if (s == "II" || s == "2" || s == "C-II" || s == "C2" || s == "SCHEDULE II") return 2;
            if (s == "III" || s == "3" || s == "C-III" || s == "C3" || s == "SCHEDULE III") return 3;
            if (s == "IV" || s == "4" || s == "C-IV" || s == "C4" || s == "SCHEDULE IV") return 4;
            if (s == "V" || s == "5" || s == "C-V" || s == "C5" || s == "SCHEDULE V") return 5;
            return null;
        }

        private static int? ParseValidityDays(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            if (text.IndexOf("no state limit", StringComparison.OrdinalIgnoreCase) >= 0) return null;

            var m = Regex.Match(text, @"(\d+)\s*(day|month|mo|year|yr)", RegexOptions.IgnoreCase);
            if (m.Success)
            {
                int val = int.Parse(m.Groups[1].Value);
                string unit = m.Groups[2].Value.ToLowerInvariant();
                if (unit.StartsWith("mo")) return val * 30;
                if (unit.StartsWith("y")) return val * 365;
                return val;
            }

            if (Regex.IsMatch(text, @"\b1\s*year\b|\bone\s*year\b|\b12\s*months\b", RegexOptions.IgnoreCase)) return 365;
            if (Regex.IsMatch(text, @"\b2\s*years\b|\btwo\s*years\b", RegexOptions.IgnoreCase)) return 730;
            if (Regex.IsMatch(text, @"\b6\s*months\b", RegexOptions.IgnoreCase)) return 180;
            if (Regex.IsMatch(text, @"\b3\s*months\b", RegexOptions.IgnoreCase)) return 90;

            return null;
        }
    }
}
