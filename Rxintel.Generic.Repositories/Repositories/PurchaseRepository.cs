using Microsoft.Extensions.Logging;
using Rxintel.Generic.Entities.Entities;
using Rxintel.Generic.Repositories.Interfaces;
namespace Rxintel.Generic.Repositories.Repositories
{
    public class PurchaseRepository : GenericRepository<Purchase, RxintelDbContext>, IPurchaseRepository
    {
        public PurchaseRepository(RxintelDbContext context, ILogger<PurchaseRepository> logger) : base(context, logger)
        {
        }
    }
}
