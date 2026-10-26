using Microsoft.Extensions.Logging;
using Rxintel.Generic.Entities.Entities;
using Rxintel.Generic.Repositories.Interfaces;
namespace Rxintel.Generic.Repositories.Repositories
{
    public class PharmacyUserRepository : GenericRepository<PharmacyUser, RxintelDbContext>, IPharmacyUserRepository
    {
        public PharmacyUserRepository(RxintelDbContext context, ILogger<PharmacyUserRepository> logger) : base(context, logger)
        {
        }
    }
}
