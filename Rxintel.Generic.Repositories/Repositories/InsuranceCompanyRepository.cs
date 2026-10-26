using Microsoft.Extensions.Logging;
using Rxintel.Generic.Entities.Entities;
using Rxintel.Generic.Repositories.Interfaces;

namespace Rxintel.Generic.Repositories.Repositories;

public class InsuranceCompanyRepository : GenericRepository<InsuranceCompany, RxintelDbContext>, IInsuranceCompanyRepository
{
    public InsuranceCompanyRepository(RxintelDbContext context, ILogger<InsuranceCompanyRepository> logger)
        : base(context, logger)
    {
    }
}
