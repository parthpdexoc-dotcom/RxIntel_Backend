using Microsoft.Extensions.Logging;
using Rxintel.Generic.Entities.Entities;
using Rxintel.Generic.Repositories.Interfaces;
namespace Rxintel.Generic.Repositories.Repositories
{
    public class LoginRepository : GenericRepository<Admin, RxintelDbContext>, ILoginRepository
    {
        private readonly RxintelDbContext _context;

        public LoginRepository(RxintelDbContext context, ILogger<LoginRepository> logger) : base(context, logger)
        {
            _context = context;
        }
    }
}
