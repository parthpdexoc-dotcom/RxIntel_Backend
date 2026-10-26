using Microsoft.Extensions.Logging;
using Rxintel.Generic.Entities.Entities;
using Rxintel.Generic.Repositories.Interfaces;

namespace Rxintel.Generic.Repositories.Repositories;

public class ImportErrorFileRepository : GenericRepository<ImportErrorFile, RxintelDbContext>, IImportErrorFileRepository
{
    public ImportErrorFileRepository(RxintelDbContext context, ILogger<ImportErrorFileRepository> logger)
        : base(context, logger)
    {
    }
}
