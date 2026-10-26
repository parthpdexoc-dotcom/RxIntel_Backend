using Microsoft.Extensions.Logging;
using Rxintel.Generic.Entities.Entities;
using Rxintel.Generic.Repositories.Interfaces;
namespace Rxintel.Generic.Repositories.Repositories
{
    public class BillingFieldMappingRepository : GenericRepository<BillingFieldMapping, RxintelDbContext>, IBillingFieldMappingRepository
    {
        public BillingFieldMappingRepository(RxintelDbContext context, ILogger<BillingFieldMappingRepository> logger) : base(context, logger)
        {
        }
    }
}
