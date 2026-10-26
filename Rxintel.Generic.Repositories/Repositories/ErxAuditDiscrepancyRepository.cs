using Microsoft.Extensions.Logging;
using Rxintel.Generic.Entities.Entities;
using Rxintel.Generic.Repositories.Interfaces;

namespace Rxintel.Generic.Repositories.Repositories;

public class ErxAuditDiscrepancyRepository : GenericRepository<ErxAuditDiscrepancy, RxintelDbContext>, IErxAuditDiscrepancyRepository
{
    public ErxAuditDiscrepancyRepository(RxintelDbContext context, ILogger<ErxAuditDiscrepancyRepository> logger)
        : base(context, logger)
    {
    }
}
