using Microsoft.Extensions.Logging;
using Rxintel.Generic.Entities.Entities;
using Rxintel.Generic.Repositories.Interfaces;

namespace Rxintel.Generic.Repositories.Repositories
{
    public class ErxSigMappingRepository
        : GenericRepository<ErxSigMapping, RxintelDbContext>, IErxSigMappingRepository
    {
        public ErxSigMappingRepository(
            RxintelDbContext context,
            ILogger<GenericRepository<ErxSigMapping, RxintelDbContext>> logger)
            : base(context, logger)
        {
        }
    }
}
