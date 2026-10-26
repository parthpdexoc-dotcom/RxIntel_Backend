using Microsoft.Extensions.Logging;
using Rxintel.Generic.Entities.Entities;
using Rxintel.Generic.Repositories.Interfaces;

namespace Rxintel.Generic.Repositories.Repositories;

public class ImportErrorDetailRepository : GenericRepository<ImportErrorDetail, RxintelDbContext>, IImportErrorDetailRepository
{
    public ImportErrorDetailRepository(RxintelDbContext context, ILogger<ImportErrorDetailRepository> logger)
        : base(context, logger)
    {
    }
}
