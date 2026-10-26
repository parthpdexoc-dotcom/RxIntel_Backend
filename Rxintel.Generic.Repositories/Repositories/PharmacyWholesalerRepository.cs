using Microsoft.Extensions.Logging;
using Rxintel.Generic.Entities.Entities;
using Rxintel.Generic.Repositories.Interfaces;

namespace Rxintel.Generic.Repositories.Repositories
{
    public class PharmacyWholesalerRepository : GenericRepository<PharmacyWholesaler, RxintelDbContext>, IPharmacyWholesalerRepository
    {
        public PharmacyWholesalerRepository(RxintelDbContext context, ILogger<PharmacyWholesalerRepository> logger) : base(context, logger)
        {
        }
    }
}
