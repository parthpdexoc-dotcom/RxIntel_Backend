using Microsoft.Extensions.Logging;
using Rxintel.Generic.Entities.Entities;
using Rxintel.Generic.Repositories.Interfaces;

namespace Rxintel.Generic.Repositories.Repositories;

public class ErxAuditAlertLogRepository : GenericRepository<ErxAuditAlertLog, RxintelDbContext>, IErxAuditAlertLogRepository
{
    public ErxAuditAlertLogRepository(RxintelDbContext context, ILogger<ErxAuditAlertLogRepository> logger)
        : base(context, logger)
    {
    }
}
