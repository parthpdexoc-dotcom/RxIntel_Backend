using Microsoft.Extensions.Logging;
using Rxintel.Generic.Entities.Entities;
using Rxintel.Generic.Repositories.Interfaces;

namespace Rxintel.Generic.Repositories.Repositories;

public class ErxAuditBatchRepository : GenericRepository<ErxAuditBatch, RxintelDbContext>, IErxAuditBatchRepository
{
    public ErxAuditBatchRepository(RxintelDbContext context, ILogger<ErxAuditBatchRepository> logger)
        : base(context, logger)
    {
    }
}
