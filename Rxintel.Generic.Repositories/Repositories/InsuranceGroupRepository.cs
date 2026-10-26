using Microsoft.Extensions.Logging;
using Rxintel.Generic.Entities.Entities;
using Rxintel.Generic.Repositories.Interfaces;

namespace Rxintel.Generic.Repositories.Repositories
{
    public class InsuranceGroupRepository
        : GenericRepository<InsuranceGroup, RxintelDbContext>, IInsuranceGroupRepository
    {
        public InsuranceGroupRepository(
            RxintelDbContext context,
            ILogger<GenericRepository<InsuranceGroup, RxintelDbContext>> logger)
            : base(context, logger)
        {
        }
    }
}