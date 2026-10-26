using Microsoft.Extensions.Logging;
using Rxintel.Generic.Entities.Entities;
using Rxintel.Generic.Repositories.Interfaces;

namespace Rxintel.Generic.Repositories.Repositories;

public class ErxColumnMatchRuleRepository : GenericRepository<ErxColumnMatchRule, RxintelDbContext>, IErxColumnMatchRuleRepository
{
    public ErxColumnMatchRuleRepository(RxintelDbContext context, ILogger<ErxColumnMatchRuleRepository> logger)
        : base(context, logger)
    {
    }
}
