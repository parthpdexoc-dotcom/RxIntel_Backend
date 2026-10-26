using Microsoft.Extensions.Logging;
using Rxintel.Generic.Entities.Entities;
using Rxintel.Generic.Repositories.Interfaces;
namespace Rxintel.Generic.Repositories.Repositories
{
    public class MedicineRepository : GenericRepository<Medicine, RxintelDbContext>, IMedicineRepository
    {
        public MedicineRepository(RxintelDbContext context, ILogger<MedicineRepository> logger) : base(context, logger)
        {
        }
    }
}
