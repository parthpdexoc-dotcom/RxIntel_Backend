using Microsoft.Extensions.Logging;
using Rxintel.Generic.Entities.Entities;
using Rxintel.Generic.Repositories.Interfaces;
namespace Rxintel.Generic.Repositories.Repositories
{
    public class InsuranceRepository : GenericRepository<Insurance, RxintelDbContext>, IInsuranceRepository
    {
        public InsuranceRepository(RxintelDbContext context, ILogger<InsuranceRepository> logger) : base(context, logger)
        {
        }
    }
}
