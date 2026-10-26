using Microsoft.Extensions.Logging;
using Rxintel.Generic.Entities.Entities;
using Rxintel.Generic.Repositories.Interfaces;
namespace Rxintel.Generic.Repositories.Repositories
{
    public class WholesalerRepository : GenericRepository<Wholesaler, RxintelDbContext>, IWholesalerRepository
    {
        public WholesalerRepository(RxintelDbContext context, ILogger<WholesalerRepository> logger) : base(context, logger)
        {
        }
    }
}
