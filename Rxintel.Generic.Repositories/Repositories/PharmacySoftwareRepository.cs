using Microsoft.Extensions.Logging;
using Rxintel.Generic.Entities.Entities;
using Rxintel.Generic.Repositories.Interfaces;

namespace Rxintel.Generic.Repositories.Repositories
{
    public class PharmacySoftwareRepository : GenericRepository<PharmacyBillingSoftware, RxintelDbContext>, IPharmacySoftwareRepository
    {
        public PharmacySoftwareRepository(RxintelDbContext context, ILogger<PharmacySoftwareRepository> logger) : base(context, logger)
        {
        }
    }
}
