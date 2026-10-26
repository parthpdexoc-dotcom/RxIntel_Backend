using System.Collections.Generic;
using System.Threading.Tasks;
using Rxintel.Generic.Entities.Entities;

namespace Rxintel.Generic.Services.Interfaces
{
    public interface IErxAuditAlertService
    {
        Task LogAndSendAlertAsync(ErxAuditRecord record, IReadOnlyList<ErxAuditDiscrepancy> discrepancies, string? alertType = "InApp");
        Task LogAndSendBatchAlertsAsync(IReadOnlyList<(ErxAuditRecord Record, IReadOnlyList<ErxAuditDiscrepancy> Discrepancies)> alertItems, string? alertType = "InApp");
    }
}
