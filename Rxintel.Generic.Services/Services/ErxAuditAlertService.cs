using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Rxintel.Generic.Entities.Entities;
using Rxintel.Generic.Repositories.Interfaces;
using Rxintel.Generic.Services.Interfaces;

namespace Rxintel.Generic.Services.Services
{
    public class ErxAuditAlertService : IErxAuditAlertService
    {
        private readonly IErxAuditAlertLogRepository _alertLogRepository;
        private readonly IEmailService _emailService;
        private readonly IAdminRepository _adminRepository;
        private readonly ILogger<ErxAuditAlertService> _logger;

        public ErxAuditAlertService(
            IErxAuditAlertLogRepository alertLogRepository,
            IEmailService emailService,
            IAdminRepository adminRepository,
            ILogger<ErxAuditAlertService> logger)
        {
            _alertLogRepository = alertLogRepository;
            _emailService = emailService;
            _adminRepository = adminRepository;
            _logger = logger;
        }

        public async Task LogAndSendAlertAsync(ErxAuditRecord record, IReadOnlyList<ErxAuditDiscrepancy> discrepancies, string? alertType = "InApp")
        {
            if (record == null || discrepancies == null || discrepancies.Count == 0) return;

            var sb = new StringBuilder();
            sb.Append($"RX# {record.RxNumber}: Mismatch found in ");

            var parts = discrepancies.Select(d =>
            {
                var fieldName = d.Rule?.FieldGroupName ?? "Field";
                return $"{fieldName} — MicroMerchant: '{d.MmValue}' vs ERX: '{d.ErxValue}'";
            });

            sb.Append(string.Join("; ", parts));
            var alertMessage = sb.ToString();

            var alertLog = new ErxAuditAlertLog
            {
                RecordId = record.RecordId,
                AlertMessage = alertMessage,
                SentAt = DateTime.UtcNow,
                AlertType = string.IsNullOrWhiteSpace(alertType) ? "InApp" : alertType
            };

            await _alertLogRepository.AddAsync(alertLog);

            _logger.LogInformation("ERx Audit Alert logged for Rx {RxNumber}: {Message}", record.RxNumber, alertMessage);

            if (string.Equals(alertType, "Email", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var admins = (await _adminRepository.GetAllAsync()).ToList();
                    var recipientEmails = admins
                        .Where(a => !string.IsNullOrWhiteSpace(a.Email))
                        .Select(a => a.Email.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase);

                    foreach (var email in recipientEmails)
                    {
                        await _emailService.SendEmail(
                            email,
                            "Pharmacy Admin",
                            $"[ERx Audit Alert] Discrepancy on RX# {record.RxNumber}",
                            $"<p>A discrepancy was detected during ERx Audit Comparison for <strong>RX# {record.RxNumber}</strong>:</p><p>{alertMessage}</p>");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to dispatch email alert for Rx {RxNumber}", record.RxNumber);
                }
            }
        }

        public async Task LogAndSendBatchAlertsAsync(IReadOnlyList<(ErxAuditRecord Record, IReadOnlyList<ErxAuditDiscrepancy> Discrepancies)> alertItems, string? alertType = "InApp")
        {
            if (alertItems == null || alertItems.Count == 0) return;

            var alertLogs = new List<ErxAuditAlertLog>();

            foreach (var item in alertItems)
            {
                if (item.Record == null || item.Discrepancies == null || item.Discrepancies.Count == 0) continue;

                var sb = new StringBuilder();
                sb.Append($"RX# {item.Record.RxNumber}: Mismatch found in ");

                var parts = item.Discrepancies.Select(d =>
                {
                    var fieldName = d.Rule?.FieldGroupName ?? "Field";
                    return $"{fieldName} — MicroMerchant: '{d.MmValue}' vs ERX: '{d.ErxValue}'";
                });

                sb.Append(string.Join("; ", parts));
                var alertMessage = sb.ToString();

                alertLogs.Add(new ErxAuditAlertLog
                {
                    RecordId = item.Record.RecordId,
                    AlertMessage = alertMessage,
                    SentAt = DateTime.UtcNow,
                    AlertType = string.IsNullOrWhiteSpace(alertType) ? "InApp" : alertType
                });
            }

            if (alertLogs.Count > 0)
            {
                try
                {
                    await _alertLogRepository.AddRangeAsync(alertLogs);
                    _logger.LogInformation("ERx Audit Alert logged {Count} alerts in bulk.", alertLogs.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to bulk persist {Count} alert logs: {Message}", alertLogs.Count, ex.Message);
                }
            }
        }
    }
}
