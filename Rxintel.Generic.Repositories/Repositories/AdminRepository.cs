using Microsoft.Extensions.Logging;
using Rxintel.Generic.Entities.Entities;
using Rxintel.Generic.Repositories.Interfaces;
namespace Rxintel.Generic.Repositories.Repositories
{
      public class AdminRepository : GenericRepository<Admin, RxintelDbContext>, IAdminRepository
    {
        public AdminRepository(RxintelDbContext context,ILogger<AdminRepository> logger) : base(context, logger)
        {
        }
    }
}
