using Microsoft.Extensions.Logging;
using Rxintel.Generic.Entities.Entities;
using Rxintel.Generic.Repositories.Interfaces;

namespace Rxintel.Generic.Repositories.Repositories;

public class ErxAuditRecordRepository : GenericRepository<ErxAuditRecord, RxintelDbContext>, IErxAuditRecordRepository
{
    public ErxAuditRecordRepository(RxintelDbContext context, ILogger<ErxAuditRecordRepository> logger)
        : base(context, logger)
    {
    }
}
