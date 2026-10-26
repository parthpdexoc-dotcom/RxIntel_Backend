using Microsoft.Extensions.Logging;
using Rxintel.Generic.Entities.Entities;
using Rxintel.Generic.Repositories.Interfaces;
namespace Rxintel.Generic.Repositories.Repositories
{
    public class BillingRepository : GenericRepository<Billing, RxintelDbContext>, IBillingRepository
    {
        public BillingRepository(RxintelDbContext context, ILogger<BillingRepository> logger) : base(context, logger)
        {
        }
    }
}
