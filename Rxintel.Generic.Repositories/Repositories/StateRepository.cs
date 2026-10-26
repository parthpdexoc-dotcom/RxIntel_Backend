using Microsoft.Extensions.Logging;
using Rxintel.Generic.Entities.Entities;
using Rxintel.Generic.Repositories.Interfaces;

namespace Rxintel.Generic.Repositories.Repositories
{
    public class StateRepository : GenericRepository<State, RxintelDbContext>, IStateRepository
    {
        public StateRepository(RxintelDbContext context, ILogger<StateRepository> logger) : base(context, logger)
        {
        }
    }
}
