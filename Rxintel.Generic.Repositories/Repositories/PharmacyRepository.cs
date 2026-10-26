using Microsoft.Extensions.Logging;
using Rxintel.Generic.Entities.Entities;
using Rxintel.Generic.Repositories.Interfaces;
namespace Rxintel.Generic.Repositories.Repositories
{
    public class PharmacyRepository : GenericRepository<Pharmacy, RxintelDbContext>, IPharmacyRepository
    {
        public PharmacyRepository(RxintelDbContext context, ILogger<PharmacyRepository> logger) : base(context, logger)
        {
        }
    }
}
