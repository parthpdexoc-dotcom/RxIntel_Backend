using Microsoft.Extensions.Logging;
using Rxintel.Generic.Entities.Entities;
using Rxintel.Generic.Repositories.Interfaces;
namespace Rxintel.Generic.Repositories.Repositories
{
    public class WholesalerFieldMappingRepository : GenericRepository<WholesalerFieldMapping, RxintelDbContext>, IWholesalerFieldMappingRepository
    {
        public WholesalerFieldMappingRepository(RxintelDbContext context, ILogger<WholesalerFieldMappingRepository> logger) : base(context, logger)
        {
        }
    }
}
