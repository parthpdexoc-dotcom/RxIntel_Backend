using Microsoft.Extensions.Logging;
using Rxintel.Generic.Entities.Entities;
using Rxintel.Generic.Repositories.Interfaces;

namespace Rxintel.Generic.Repositories.Repositories
{
    public class NDCGroupRepository: GenericRepository<NDCGroup, RxintelDbContext>, INDCGroupRepository
    {
        public NDCGroupRepository(RxintelDbContext context,ILogger<GenericRepository<NDCGroup, RxintelDbContext>> logger): base(context, logger)
        {
        }
    }
}